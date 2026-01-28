using System.Collections.Concurrent;
using System.Threading.Channels;

namespace ImmichFrame.WebApi.Services;

public interface IGeneralSettingsChangeNotifier
{
    long CurrentVersion { get; }
    void NotifyChanged(long version);
    ChannelReader<long> Subscribe(CancellationToken ct);
}

/// <summary>
/// In-memory broadcaster for general settings changes (best-effort, per-process).
/// </summary>
public sealed class GeneralSettingsChangeNotifier : IGeneralSettingsChangeNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<long>> _subscribers = new();
    private long _currentVersion;

    public long CurrentVersion => Volatile.Read(ref _currentVersion);

    public void NotifyChanged(long version)
    {
        Volatile.Write(ref _currentVersion, version);
        foreach (var kv in _subscribers)
        {
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


