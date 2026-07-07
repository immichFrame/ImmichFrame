namespace ImmichFrame.Core.Models;

public class CustomWidgetData
{
    public string Title { get; set; } = string.Empty;
    public List<CustomWidgetItem> Items { get; set; } = new();
}

public class CustomWidgetItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Secondary { get; set; }
}
