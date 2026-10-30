using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ImmichFrame.Core.Helpers;

/// <summary>
/// A value loaded on demand and kept for a lifetime. Concurrent callers share one load. Once the
/// value is older than its lifetime, callers keep getting it while a single refresh runs in the
/// background. A caller's cancellation ends only its own wait, never the shared load.
/// After a failed load the next attempt waits, starting at <see cref="InitialBackoff"/> and
/// doubling up to the smaller of the lifetime and <see cref="MaxBackoff"/> (<see cref="MaxBackoffWithoutValue"/>
/// while no value has loaded yet); until then a caller without a value gets the last failure.
/// </summary>
public sealed class RefreshingValue<T> : IDisposable
{
    public static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(30);

    /// <summary>The longest wait between attempts, so a value comes back soon after its source does.</summary>
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(15);

    /// <summary>The longest wait between attempts while there is no value to serve meanwhile.</summary>
    public static readonly TimeSpan MaxBackoffWithoutValue = TimeSpan.FromMinutes(5);

    private readonly Func<CancellationToken, Task<T>> _load;
    private readonly TimeSpan _lifetime;
    private readonly TimeSpan _maxAge;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly string _description;
    private readonly CancellationTokenSource _disposed = new();
    private readonly object _gate = new();

    private T? _value;
    private bool _hasValue;
    private DateTimeOffset _loadedAt;
    private Task<T>? _loading;

    private int _failures;
    private DateTimeOffset _retryAt = DateTimeOffset.MinValue;
    private Exception? _lastError;
    private bool _staleWarned;

    /// <param name="maxAge">
    /// Past this age a served value is logged once as stale. Defaults to twice the lifetime.
    /// </param>
    /// <param name="description">What the value is, for log messages.</param>
    public RefreshingValue(Func<CancellationToken, Task<T>> load, TimeSpan lifetime, TimeProvider? timeProvider = null,
        ILogger? logger = null, TimeSpan? maxAge = null, string description = "cached value")
    {
        _load = load;
        _lifetime = lifetime;
        _maxAge = maxAge ?? (lifetime > TimeSpan.MaxValue / 2 ? TimeSpan.MaxValue : lifetime * 2);
        _time = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;
        _description = description;
    }

    public TimeSpan Lifetime => _lifetime;

    public Task<T> GetAsync(CancellationToken ct = default)
    {
        Task<T>? loading = null;
        TimeSpan? staleAge = null;
        T value = default!;
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            if (_hasValue)
            {
                var age = now - _loadedAt;
                if (_loading == null && age >= _lifetime && now >= _retryAt)
                {
                    _loading = StartLoad();
                    // Nobody awaits a background refresh; observe its failure so it is not rethrown later.
                    _loading.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                }

                if (!_staleWarned && age > _maxAge)
                {
                    _staleWarned = true;
                    staleAge = age;
                }

                value = _value!;
            }
            else if (_loading == null && now < _retryAt)
            {
                return Task.FromException<T>(_lastError!);
            }
            else
            {
                loading = _loading ??= StartLoad();
            }
        }

        if (loading != null)
        {
            return loading.WaitAsync(ct);
        }

        if (staleAge is { } stale)
        {
            _logger.LogWarning("Still serving the {description} loaded {age} ago, because refreshing it keeps failing", _description, stale);
        }

        return Task.FromResult(value);
    }

    // Runs the load on the thread pool so none of it runs while _gate is held.
    private Task<T> StartLoad() => Task.Run(Load);

    private async Task<T> Load()
    {
        try
        {
            var value = await _load(_disposed.Token);
            lock (_gate)
            {
                _value = value;
                _hasValue = true;
                _loadedAt = _time.GetUtcNow();
                _loading = null;
                _failures = 0;
                _retryAt = DateTimeOffset.MinValue;
                _lastError = null;
                _staleWarned = false;
            }

            return value;
        }
        catch (Exception e)
        {
            TimeSpan? delay = null;
            lock (_gate)
            {
                _loading = null;
                if (!_disposed.IsCancellationRequested)
                {
                    _failures++;
                    delay = Backoff(_failures);
                    _retryAt = _time.GetUtcNow() + delay.Value;
                    _lastError = e;
                }
            }

            if (delay is { } wait)
            {
                _logger.LogWarning(e, "Could not load the {description}; trying again in {delay}", _description, wait);
            }

            throw;
        }
    }

    // InitialBackoff doubled for each failure in a row, capped as described on the class. Caller holds _gate.
    private TimeSpan Backoff(int failures)
    {
        var cap = _hasValue ? MaxBackoff : MaxBackoffWithoutValue;
        if (_lifetime < cap)
        {
            cap = _lifetime;
        }

        var delay = InitialBackoff;
        for (var i = 1; i < failures && delay < cap; i++)
        {
            delay *= 2;
        }

        return delay < cap ? delay : cap;
    }

    // Cancels a load still running. The token source is not disposed, since that load may still hold its token.
    public void Dispose() => _disposed.Cancel();
}
