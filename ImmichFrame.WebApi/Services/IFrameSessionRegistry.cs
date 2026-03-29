using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

public enum FrameSessionCommandEnqueueStatus
{
    Enqueued,
    NotFound,
    Stale
}

public class FrameSessionCommandEnqueueResult
{
    public required FrameSessionCommandEnqueueStatus Status { get; init; }
    public AdminCommandDto? Command { get; init; }
}

public interface IFrameSessionRegistry
{
    void UpsertSnapshot(string clientIdentifier, FrameSessionSnapshotDto snapshot, string? userAgent);
    IReadOnlyList<AdminCommandDto> GetPendingCommands(string clientIdentifier);
    bool AcknowledgeCommand(string clientIdentifier, long commandId);
    bool MarkStopped(string clientIdentifier, string? userAgent);
    FrameSessionCommandEnqueueResult EnqueueCommand(string clientIdentifier, FrameAdminCommandType commandType);
    bool UpdateDisplayName(string clientIdentifier, string? displayName);
    IReadOnlyList<FrameSessionStateDto> GetActiveSessions();
}
