using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace ImmichFrame.ViewModels;

public class ViewModelBase : ObservableObject
{
    public Func<string, string, Task>? ShowMessageBoxFromThread;
    public Func<string, string, Task>? ShowMessageBox;
    public Func<UserControl>? GetUserControl;

    public virtual void UpdateMargin(string margin)
    {
        MarginUpdated?.Invoke(this, new MarginUpdatedEventArgs(margin));
    }

    public delegate void MarginUpdatedEventHandler(object sender, MarginUpdatedEventArgs e);
    public event MarginUpdatedEventHandler? MarginUpdated;
}

public class NavigatableViewModelBase : ViewModelBase
{
    public virtual void Navigate(NavigatableViewModelBase viewModel)
    {
        Disposed?.Invoke(this, new EventArgs());
        Navigated?.Invoke(this, new NavigatedEventArgs(viewModel));
    }

    public delegate void NavigatedEventHandler(object sender, NavigatedEventArgs e);
    public event NavigatedEventHandler? Navigated;
    public event EventHandler? Disposed;
}
public class NavigatedEventArgs : EventArgs
{
    public NavigatedEventArgs(NavigatableViewModelBase viewModel)
    {
        ViewModel = viewModel;
    }
    public NavigatableViewModelBase ViewModel { get; set; }
}
public class MarginUpdatedEventArgs : EventArgs
{
    public MarginUpdatedEventArgs(string margin)
    {
        Margin = margin;
    }
    public string Margin { get; set; }
}
