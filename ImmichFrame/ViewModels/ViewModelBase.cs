using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ImmichFrame.ViewModels;

public class ViewModelBase : ObservableObject
{

}

public class NavigatableViewModelBase : ViewModelBase
{
    public virtual void Navigate(NavigatableViewModelBase viewModel)
    {
        Navigated?.Invoke(this, new NavigatedEventArgs(viewModel));
    }

    public delegate void NavigatedEventHandler(object sender, NavigatedEventArgs e);
    public event NavigatedEventHandler? Navigated;
}
public class NavigatedEventArgs : EventArgs
{
    public NavigatedEventArgs(NavigatableViewModelBase viewModel)
    {
        ViewModel = viewModel;
    }
    public NavigatableViewModelBase ViewModel { get; set; }
}
