using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ImmichFrame.Views
{
    public partial class SettingsView : BaseView
    {
        private ScrollViewer? _scrollViewer;
        public SettingsView()
        {
            InitializeComponent();
            _scrollViewer = this.FindControl<ScrollViewer>("scrollViewer");
            var stackPanel = this.FindControl<StackPanel>("stackPanel");
            if(stackPanel != null )
            {
                stackPanel.AddHandler(GotFocusEvent, GotFocus_Handler!, RoutingStrategies.Bubble);
            }
        }
        private void GotFocus_Handler(object sender, GotFocusEventArgs e)
        {
            if (e.Source is TextBox textBox)
            {
                var textBoxPosition = textBox.TranslatePoint(new Point(0, 0), _scrollViewer!);
                if (textBoxPosition.HasValue)
                {
                    var targetOffsetY = _scrollViewer!.Offset.Y + textBoxPosition.Value.Y - 40;
                    _scrollViewer.Offset = new Vector(0, targetOffsetY);
                }
            }
        }        
    }
}
