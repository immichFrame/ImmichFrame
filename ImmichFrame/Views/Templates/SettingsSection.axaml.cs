using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace ImmichFrame.Views.Templates;
public class SettingsSection : TemplatedControl
{
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<Panel>();

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set
        {
            if (GetValue(ContentProperty) is ILogical oldLogical) LogicalChildren.Remove(oldLogical);
            SetValue(ContentProperty, value);
            if (value is ILogical newLogical) LogicalChildren.Add(newLogical);
        }
    }
}

