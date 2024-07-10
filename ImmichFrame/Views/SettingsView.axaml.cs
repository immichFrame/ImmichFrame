using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ImmichFrame.Helpers;
using ImmichFrame.ViewModels;
using System;

namespace ImmichFrame.Views
{
    public partial class SettingsView : BaseView
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged!;
            this.MessageBoxClosed += ReturnFromMessageBox!;
        }
        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.ListItemRemoved += ViewModel_ListItemRemoved!;
            }
        }
        private void ViewModel_ListItemRemoved(object sender, EventArgs e)
        {
            FocusOnControl();
        }
        private void UserControl_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            FocusOnControl();
        }
        private void ReturnFromMessageBox(object sender, EventArgs e)
        {
            FocusOnControl();
        }
        private void FocusOnControl()
        {
            Dispatcher.UIThread.Post(() =>
            {
                txtImmichServerUrl.Focus();
            });
        }

        private void GotFocus_Handler(object sender, GotFocusEventArgs e)
        {
            if (e.Source is TextBox textBox)
            {
                ScrollToControl(textBox);
            }
            else if (e.Source is NumericUpDown numericUpDown)
            {
                ScrollToControl(numericUpDown);
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
        private void On_KeyDown(object sender, KeyEventArgs e)
        {
            KeyEventArgs tabKeyEvent;
            switch (e.Key)
            {
                case Key.Up:
                    tabKeyEvent = new KeyEventArgs
                    {
                        RoutedEvent = InputElement.KeyDownEvent,
                        Key = Key.Tab,
                        KeyModifiers = KeyModifiers.Shift
                    };
                    (sender as InputElement)?.RaiseEvent(tabKeyEvent);
                    e.Handled = true;
                    break;

                case Key.Down:
                    tabKeyEvent = new KeyEventArgs
                    {
                        RoutedEvent = InputElement.KeyDownEvent,
                        Key = Key.Tab,
                        KeyModifiers = KeyModifiers.None
                    };
                    (sender as InputElement)?.RaiseEvent(tabKeyEvent);
                    e.Handled = true;
                    break;
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
