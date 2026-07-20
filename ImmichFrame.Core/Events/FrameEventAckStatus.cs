using System.Text.Json.Serialization;

namespace ImmichFrame.Core.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FrameEventAckStatus
{
    Shown,
    Closed,
    Timeout,
    Error,
    Dismissed
}
