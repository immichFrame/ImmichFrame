using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

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

                var scrollViewer = this.FindControl<ScrollViewer>("scrollViewer");
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
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
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var scrollViewer = this.FindControl<ScrollViewer>("scrollViewer");
            if (scrollViewer != null)
            {
                scrollViewer.Offset = new Vector(0, 0);
            }
        }

    }
}
