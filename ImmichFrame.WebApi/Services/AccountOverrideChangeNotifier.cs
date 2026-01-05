using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ImmichFrame.WebApi.Services;

public interface IAccountOverrideChangeNotifier
{
    long CurrentVersion { get; }
    void NotifyChanged(long version);
    ChannelReader<long> Subscribe(CancellationToken ct);
}

/// <summary>
/// In-memory broadcaster for account override changes (best-effort, per-process).
/// Used to notify web clients (SSE) to refresh immediately after admin updates.
/// </summary>
public sealed class AccountOverrideChangeNotifier : IAccountOverrideChangeNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<long>> _subscribers = new();
    private long _currentVersion;

    public long CurrentVersion => Volatile.Read(ref _currentVersion);

    public void NotifyChanged(long version)
    {
        Volatile.Write(ref _currentVersion, version);

        foreach (var kv in _subscribers)
        {
            // Best-effort; if a subscriber is slow it will buffer.
            kv.Value.Writer.TryWrite(version);
        }
    }

    public ChannelReader<long> Subscribe(CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true
        });

        _subscribers.TryAdd(id, channel);

        ct.Register(() =>
        {
            if (_subscribers.TryRemove(id, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });

        return channel.Reader;
    }
}


