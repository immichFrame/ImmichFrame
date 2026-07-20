namespace ImmichFrame.Core.Events;

public class FrameEventAction
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Kind { get; init; }
}
