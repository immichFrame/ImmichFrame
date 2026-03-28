using ImmichFrame.WebApi.Models;

namespace ImmichFrame.WebApi.Services;

public interface IFrameSessionRegistry
{
    void UpsertSnapshot(string clientIdentifier, FrameSessionSnapshotDto snapshot, string? userAgent);
    IReadOnlyList<AdminCommandDto> GetPendingCommands(string clientIdentifier);
    bool AcknowledgeCommand(string clientIdentifier, long commandId);
    bool MarkStopped(string clientIdentifier, string? userAgent);
    AdminCommandDto? EnqueueCommand(string clientIdentifier, FrameAdminCommandType commandType);
    bool UpdateDisplayName(string clientIdentifier, string? displayName);
    IReadOnlyList<FrameSessionStateDto> GetActiveSessions();
}
