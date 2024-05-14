using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Helpers;
using System;
using System.Windows.Input;

namespace ImmichFrame.ViewModels;

public partial class ErrorViewModel : NavigatableViewModelBase
{

    [ObservableProperty]
    private Exception ex;
    public ICommand QuitCommand { get; set; }
    public ICommand CopyCommand { get; set; }
    public ICommand LogCommand { get; set; }

    public ErrorViewModel() : this(new NotImplementedException())
    {

    }

    public ErrorViewModel(Exception ex)
    {
        Ex = ex;
        QuitCommand = new RelayCommand(QuitAction);
        CopyCommand = new RelayCommand(QuitAction);
        LogCommand = new RelayCommand(QuitAction);
    }

    public void QuitAction()
    {
        Environment.Exit(0);
    }
}