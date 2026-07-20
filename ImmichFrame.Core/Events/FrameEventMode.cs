using System.Text.Json.Serialization;

namespace ImmichFrame.Core.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FrameEventMode
{
    PopupText,
    Close,
    Banner
}
