namespace ImmichFrame.WebApi.Helpers;

/// <summary>
/// Holds a unique session ID generated at server startup.
/// Clients compare it against their persisted value to detect a server restart
/// and clear stale persisted assets (which the restarted server can no longer route).
/// </summary>
public class ServerSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
}
