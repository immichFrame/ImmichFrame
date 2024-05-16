using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace ImmichFrame.ViewModels;

public class ViewModelBase : ObservableObject
{
    public Func<string, string, Task> ShowMessageBoxFromThread;
    public Func<string, string, Task> ShowMessageBox;
    public Func<UserControl> GetUserControl;
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
