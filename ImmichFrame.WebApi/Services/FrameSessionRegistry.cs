using System.Text.Json;
using ImmichFrame.WebApi.Models;
using Microsoft.Extensions.Logging;

namespace ImmichFrame.WebApi.Services;

public class FrameSessionRegistryOptions
{
    public TimeSpan HeartbeatTtl { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan StaleSessionTtl { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxHistoryItems { get; init; } = 50;
    public string? DisplayNameStorePath { get; init; }
}

public class FrameSessionRegistry : IFrameSessionRegistry
{
    private readonly Dictionary<string, FrameSessionRecord> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _persistedDisplayNames = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly FrameSessionRegistryOptions _options;
    private readonly ILogger<FrameSessionRegistry>? _logger;
    private long _nextCommandId;

    public FrameSessionRegistry()
        : this(new FrameSessionRegistryOptions(), null, null)
    {
    }

    internal FrameSessionRegistry(FrameSessionRegistryOptions options, Func<DateTimeOffset>? utcNow, ILogger<FrameSessionRegistry>? logger = null)
    {
        _options = options;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _logger = logger;
        LoadPersistedDisplayNames();
    }

    public void UpsertSnapshot(string clientIdentifier, FrameSessionSnapshotDto snapshot, string? userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdentifier);
        ArgumentNullException.ThrowIfNull(snapshot);

        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                session = new FrameSessionRecord
                {
                    ClientIdentifier = clientIdentifier,
                    ConnectedAtUtc = now,
                    DisplayName = _persistedDisplayNames.GetValueOrDefault(clientIdentifier)
                };
                _sessions[clientIdentifier] = session;
            }

            session.LastSeenAtUtc = now;
            session.UserAgent = string.IsNullOrWhiteSpace(userAgent) ? session.UserAgent : userAgent;
            session.PlaybackState = snapshot.PlaybackState;
            session.Status = snapshot.Status;
            if (!string.IsNullOrWhiteSpace(snapshot.DisplayName))
            {
                ApplyDisplayNameUnsafe(session, NormalizeDisplayName(snapshot.DisplayName), persistWhenProvided: true);
            }
            session.CurrentDisplay = CloneDisplayEvent(snapshot.CurrentDisplay);
            session.History = (snapshot.History ?? Enumerable.Empty<DisplayEventDto>())
                .Take(_options.MaxHistoryItems)
                .Select(CloneDisplayEvent)
                .Where(x => x != null)
                .Cast<DisplayEventDto>()
                .ToList();
        }
    }

    public IReadOnlyList<AdminCommandDto> GetPendingCommands(string clientIdentifier)
    {
        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                return Array.Empty<AdminCommandDto>();
            }

            return session.PendingCommands.Select(CloneCommand).ToList();
        }
    }

    public bool AcknowledgeCommand(string clientIdentifier, long commandId)
    {
        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                return false;
            }

            var removed = session.PendingCommands.RemoveAll(command => command.CommandId <= commandId);
            return removed > 0;
        }
    }

    public bool MarkStopped(string clientIdentifier, string? userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdentifier);

        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                session = new FrameSessionRecord
                {
                    ClientIdentifier = clientIdentifier,
                    ConnectedAtUtc = now,
                    DisplayName = _persistedDisplayNames.GetValueOrDefault(clientIdentifier)
                };
                _sessions[clientIdentifier] = session;
            }

            session.LastSeenAtUtc = now;
            session.Status = FrameSessionStatus.Stopped;
            session.PlaybackState = FramePlaybackState.Paused;
            session.UserAgent = string.IsNullOrWhiteSpace(userAgent) ? session.UserAgent : userAgent;
            return true;
        }
    }

    public FrameSessionCommandEnqueueResult EnqueueCommand(string clientIdentifier, FrameAdminCommandType commandType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientIdentifier);

        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                return new FrameSessionCommandEnqueueResult
                {
                    Status = FrameSessionCommandEnqueueStatus.NotFound
                };
            }

            if (session.Status != FrameSessionStatus.Active || now - session.LastSeenAtUtc > _options.HeartbeatTtl)
            {
                return new FrameSessionCommandEnqueueResult
                {
                    Status = FrameSessionCommandEnqueueStatus.Stale
                };
            }

            var command = new AdminCommandDto
            {
                CommandId = ++_nextCommandId,
                CommandType = commandType,
                IssuedAtUtc = now
            };

            session.PendingCommands.Add(command);
            return new FrameSessionCommandEnqueueResult
            {
                Status = FrameSessionCommandEnqueueStatus.Enqueued,
                Command = CloneCommand(command)
            };
        }
    }

    public bool UpdateDisplayName(string clientIdentifier, string? displayName)
    {
        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            if (!_sessions.TryGetValue(clientIdentifier, out var session))
            {
                return false;
            }

            ApplyDisplayNameUnsafe(session, NormalizeDisplayName(displayName), persistWhenProvided: true);
            return true;
        }
    }

    public IReadOnlyList<FrameSessionStateDto> GetActiveSessions()
    {
        var now = _utcNow();
        lock (_sync)
        {
            PruneUnsafe(now);

            return _sessions.Values
                .Where(session => session.Status == FrameSessionStatus.Active)
                .Where(session => now - session.LastSeenAtUtc <= _options.HeartbeatTtl)
                .OrderByDescending(session => session.LastSeenAtUtc)
                .Select(CloneSession)
                .ToList();
        }
    }

    private void PruneUnsafe(DateTimeOffset now)
    {
        var expiredKeys = _sessions
            .Where(entry => now - entry.Value.LastSeenAtUtc > _options.StaleSessionTtl)
            .Select(entry => entry.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _sessions.Remove(key);
        }
    }

    private static FrameSessionStateDto CloneSession(FrameSessionRecord session)
    {
        return new FrameSessionStateDto
        {
            ClientIdentifier = session.ClientIdentifier,
            ConnectedAtUtc = session.ConnectedAtUtc,
            LastSeenAtUtc = session.LastSeenAtUtc,
            UserAgent = session.UserAgent,
            DisplayName = session.DisplayName,
            PlaybackState = session.PlaybackState,
            Status = session.Status,
            CurrentDisplay = CloneDisplayEvent(session.CurrentDisplay),
            History = session.History.Select(CloneDisplayEvent).Where(x => x != null).Cast<DisplayEventDto>().ToList()
        };
    }

    private static AdminCommandDto CloneCommand(AdminCommandDto command)
    {
        return new AdminCommandDto
        {
            CommandId = command.CommandId,
            CommandType = command.CommandType,
            IssuedAtUtc = command.IssuedAtUtc
        };
    }

    private static DisplayEventDto? CloneDisplayEvent(DisplayEventDto? displayEvent)
    {
        if (displayEvent == null)
        {
            return null;
        }

        return new DisplayEventDto
        {
            DisplayedAtUtc = displayEvent.DisplayedAtUtc,
            DurationSeconds = displayEvent.DurationSeconds,
            Assets = (displayEvent.Assets ?? Enumerable.Empty<DisplayedAssetDto>())
                .Select(CloneDisplayedAsset)
                .ToList()
        };
    }

    private static DisplayedAssetDto CloneDisplayedAsset(DisplayedAssetDto asset)
    {
        return new DisplayedAssetDto
        {
            Id = asset.Id,
            OriginalFileName = asset.OriginalFileName,
            Type = asset.Type,
            ImmichServerUrl = asset.ImmichServerUrl,
            LocalDateTime = asset.LocalDateTime,
            Description = asset.Description,
            Thumbhash = asset.Thumbhash
        };
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        return displayName.Trim();
    }

    private void ApplyDisplayNameUnsafe(FrameSessionRecord session, string? displayName, bool persistWhenProvided)
    {
        if (displayName == null)
        {
            if (persistWhenProvided)
            {
                session.DisplayName = null;
                _persistedDisplayNames.Remove(session.ClientIdentifier);
                SavePersistedDisplayNamesUnsafe();
            }

            return;
        }

        session.DisplayName = displayName;
        if (!persistWhenProvided)
        {
            return;
        }

        _persistedDisplayNames[session.ClientIdentifier] = displayName;
        SavePersistedDisplayNamesUnsafe();
    }

    private void LoadPersistedDisplayNames()
    {
        var path = _options.DisplayNameStorePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (values == null)
            {
                return;
            }

            foreach (var entry in values)
            {
                var normalizedName = NormalizeDisplayName(entry.Value);
                if (!string.IsNullOrWhiteSpace(entry.Key) && normalizedName != null)
                {
                    _persistedDisplayNames[entry.Key] = normalizedName;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load name-store file, continuing with empty in-memory cache.");
        }
    }

    private void SavePersistedDisplayNamesUnsafe()
    {
        var path = _options.DisplayNameStorePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_persistedDisplayNames, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private sealed class FrameSessionRecord
    {
        public required string ClientIdentifier { get; init; }
        public DateTimeOffset ConnectedAtUtc { get; set; }
        public DateTimeOffset LastSeenAtUtc { get; set; }
        public string? UserAgent { get; set; }
        public string? DisplayName { get; set; }
        public FramePlaybackState PlaybackState { get; set; } = FramePlaybackState.Playing;
        public FrameSessionStatus Status { get; set; } = FrameSessionStatus.Active;
        public DisplayEventDto? CurrentDisplay { get; set; }
        public List<DisplayEventDto> History { get; set; } = new();
        public List<AdminCommandDto> PendingCommands { get; } = new();
    }
}
