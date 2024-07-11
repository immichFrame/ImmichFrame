using Avalonia.Input;
using Avalonia.Threading;

namespace ImmichFrame.Views;

public partial class ErrorView : BaseView
{
    public ErrorView()
    {
        InitializeComponent();
        //simulate Tab to get focus on first control
        Dispatcher.UIThread.Post(() =>
        {
            KeyEventArgs tabKeyEvent = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Key = Key.Tab,
                KeyModifiers = KeyModifiers.None
            };
            this?.RaiseEvent(tabKeyEvent);
        });
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
}
