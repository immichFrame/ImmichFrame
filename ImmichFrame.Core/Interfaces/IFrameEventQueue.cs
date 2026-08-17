using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImmichFrame.Core.Events;

namespace ImmichFrame.Core.Interfaces;

public interface IFrameEventQueue
{
    Task<bool> EnqueueAsync(FrameEvent frameEvent, CancellationToken cancellationToken = default);
    Task<FrameEvent?> PeekNextAsync(string deviceId, FrameEventMode? mode = null, CancellationToken cancellationToken = default);
    Task<bool> AckAsync(string deviceId, string eventId, FrameEventAckStatus status, CancellationToken cancellationToken = default);
    Task<int> RemoveByCategoryAsync(string deviceId, string category, CancellationToken cancellationToken = default);
    IReadOnlyList<(FrameEvent Event, FrameEventAckStatus? LastAckStatus)> GetDeviceSnapshot(string deviceId);
}
