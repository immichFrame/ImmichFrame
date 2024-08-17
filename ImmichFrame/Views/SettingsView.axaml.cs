using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;

namespace ImmichFrame.Views
{
    public partial class SettingsView : BaseView
    {
        public SettingsView()
        {
            InitializeComponent();
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
            var expander = this.FindControl<Expander>("serverExpander");
            if (expander != null)
            {
                expander.IsExpanded = true;
            }
        }
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander expandedExpander)
            {
                foreach (var child in (expandedExpander.Parent as StackPanel)!.Children)
                {
                    if (child is Expander expander && expander != expandedExpander)
                    {
                        expander.IsExpanded = false;
                    }
                }
                var scrollViewer = this.FindControl<ScrollViewer>("scrollViewer");

                if (scrollViewer != null)
                {
                    var expanderPosition = expandedExpander.Bounds.Y;
                    var scrollViewerOffset = scrollViewer.Offset.Y;
                    var scrollViewerHeight = scrollViewer.Bounds.Height;

                    double newOffset = expanderPosition - (scrollViewerHeight / 2) + (expandedExpander.Bounds.Height / 2);
                    scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, newOffset));
                }
            }
        }
    }
}
