using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ImmichFrame.Views
{
    public partial class SettingsView : BaseView
    {
        public SettingsView()
        {
            InitializeComponent();
        }
        private void GotFocus_Handler(object sender, GotFocusEventArgs e)
        {
            if (e.Source is TextBox textBox)
            {
                ScrollToControl(textBox);
            }
        }
        private void ScrollToControl(Control control)
        {
            var scrollViewer = this.FindControl<ScrollViewer>("scrollViewer");
            if (scrollViewer != null)
            {
                var controlPosition = control.TranslatePoint(new Point(0, 0), scrollViewer);
                if (controlPosition.HasValue)
                {
                    var targetOffsetY = scrollViewer.Offset.Y + controlPosition.Value.Y - 40;
                    scrollViewer.Offset = new Vector(0, targetOffsetY);
                }
            }
        }
        private void NumericUpDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                var numericUpDown = (NumericUpDown)sender;
                numericUpDown.Value++;
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                var numericUpDown = (NumericUpDown)sender;
                numericUpDown.Value--;
                e.Handled = true;
            }
        }
    }
}
