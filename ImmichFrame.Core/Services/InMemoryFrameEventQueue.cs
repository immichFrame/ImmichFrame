using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImmichFrame.Core.Events;
using ImmichFrame.Core.Interfaces;

namespace ImmichFrame.Core.Services;

public class InMemoryFrameEventQueue : IFrameEventQueue
{
    private readonly ConcurrentDictionary<string, DeviceQueue> _queues = new();

    public Task<bool> EnqueueAsync(FrameEvent frameEvent, CancellationToken cancellationToken = default)
    {
        var queue = _queues.GetOrAdd(frameEvent.DeviceId, _ => new DeviceQueue());
        return Task.FromResult(queue.Enqueue(frameEvent));
    }

    public Task<FrameEvent?> PeekNextAsync(string deviceId, FrameEventMode? mode = null, CancellationToken cancellationToken = default)
    {
        if (!_queues.TryGetValue(deviceId, out var queue))
            return Task.FromResult<FrameEvent?>(null);

        return Task.FromResult(queue.PeekNext(mode));
    }

    public Task<bool> AckAsync(string deviceId, string eventId, FrameEventAckStatus status, CancellationToken cancellationToken = default)
    {
        if (!_queues.TryGetValue(deviceId, out var queue))
            return Task.FromResult(false);

        return Task.FromResult(queue.Ack(eventId, status));
    }

    public Task<int> RemoveByCategoryAsync(string deviceId, string category, CancellationToken cancellationToken = default)
    {
        if (!_queues.TryGetValue(deviceId, out var queue))
            return Task.FromResult(0);

        return Task.FromResult(queue.RemoveByCategory(category));
    }

    public IReadOnlyList<(FrameEvent Event, FrameEventAckStatus? LastAckStatus)> GetDeviceSnapshot(string deviceId)
    {
        if (!_queues.TryGetValue(deviceId, out var queue))
            return Array.Empty<(FrameEvent, FrameEventAckStatus?)>();

        return queue.GetSnapshot();
    }

    private class DeviceQueue
    {
        private readonly object _lock = new();
        private readonly SortedSet<EventEntry> _entries = new(EventEntryComparer.Instance);
        private readonly Dictionary<string, EventEntry> _byId = new();
        private readonly Dictionary<(FrameEventMode Mode, string Category), EventEntry> _byCategory = new();
        private readonly Dictionary<FrameEventMode, string> _activeEventIdByMode = new();

        public bool Enqueue(FrameEvent frameEvent)
        {
            lock (_lock)
            {
                if (frameEvent.Mode == FrameEventMode.Close)
                {
                    if (!string.IsNullOrWhiteSpace(frameEvent.Category))
                        RemoveByCategory(frameEvent.Category);
                    else
                        Clear();
                    return true;
                }

                if (_byId.ContainsKey(frameEvent.Id))
                    return false;

                if (!string.IsNullOrWhiteSpace(frameEvent.Category))
                {
                    var key = (frameEvent.Mode, frameEvent.Category.ToLowerInvariant());
                    if (_byCategory.TryGetValue(key, out var existing))
                        Remove(existing);
                }

                var entry = new EventEntry(frameEvent);
                _entries.Add(entry);
                _byId[frameEvent.Id] = entry;

                if (!string.IsNullOrWhiteSpace(frameEvent.Category))
                    _byCategory[(frameEvent.Mode, frameEvent.Category.ToLowerInvariant())] = entry;

                return true;
            }
        }

        public FrameEvent? PeekNext(FrameEventMode? mode = null)
        {
            lock (_lock)
            {
                RemoveExpired();

                if (_entries.Count == 0)
                {
                    _activeEventIdByMode.Clear();
                    return null;
                }

                EventEntry? selected;
                if (mode is null)
                {
                    selected = _entries.Min;
                }
                else
                {
                    selected = _entries.FirstOrDefault(e => e.Event.Mode == mode.Value);
                }

                if (selected is null)
                    return null;

                _activeEventIdByMode[selected.Event.Mode] = selected.Event.Id;
                return selected.Event;
            }
        }

        public bool Ack(string eventId, FrameEventAckStatus status)
        {
            lock (_lock)
            {
                if (!_byId.TryGetValue(eventId, out var entry))
                    return false;

                entry.LastAckStatus = status;

                if (status != FrameEventAckStatus.Shown)
                {
                    var mode = entry.Event.Mode;
                    Remove(entry);
                    if (_activeEventIdByMode.TryGetValue(mode, out var activeId) && activeId == eventId)
                        _activeEventIdByMode.Remove(mode);
                }

                return true;
            }
        }

        public int RemoveByCategory(string category)
        {
            lock (_lock)
            {
                var toRemove = _entries.Where(e =>
                    string.Equals(e.Event.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var entry in toRemove)
                    Remove(entry);

                return toRemove.Count;
            }
        }

        public IReadOnlyList<(FrameEvent Event, FrameEventAckStatus? LastAckStatus)> GetSnapshot()
        {
            lock (_lock)
            {
                return _entries.Select(e => (e.Event, e.LastAckStatus)).ToList();
            }
        }

        private void Remove(EventEntry entry)
        {
            _entries.Remove(entry);
            _byId.Remove(entry.Event.Id);
            if (!string.IsNullOrWhiteSpace(entry.Event.Category))
                _byCategory.Remove((entry.Event.Mode, entry.Event.Category.ToLowerInvariant()));
        }

        private void Clear()
        {
            _entries.Clear();
            _byId.Clear();
            _byCategory.Clear();
            _activeEventIdByMode.Clear();
        }

        private void RemoveExpired()
        {
            var now = DateTime.UtcNow;
            var expired = _entries.Where(e =>
            {
                if (e.Event.TimeoutMs is not > 0) return false;
                return e.Event.PostedAt.AddMilliseconds(e.Event.TimeoutMs.Value) < now;
            }).ToList();

            foreach (var entry in expired)
                Remove(entry);
        }
    }

    private class EventEntry
    {
        public FrameEvent Event { get; }
        public FrameEventAckStatus? LastAckStatus { get; set; }

        public EventEntry(FrameEvent frameEvent) => Event = frameEvent;
    }

    private class EventEntryComparer : IComparer<EventEntry>
    {
        public static readonly EventEntryComparer Instance = new();

        public int Compare(EventEntry? x, EventEntry? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var priorityCompare = x.Event.Priority.CompareTo(y.Event.Priority);
            if (priorityCompare != 0) return priorityCompare;

            var timeCompare = x.Event.PostedAt.CompareTo(y.Event.PostedAt);
            if (timeCompare != 0) return timeCompare;

            return string.Compare(x.Event.Id, y.Event.Id, StringComparison.Ordinal);
        }
    }
}
