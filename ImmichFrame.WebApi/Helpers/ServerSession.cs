namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Holds a unique session ID generated at server startup.
/// Used by clients to detect server restarts and clear stale persisted data.
/// </summary>
public class ServerSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
}
