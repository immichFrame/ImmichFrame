using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.Windows.Input;

namespace ImmichFrame.ViewModels;

public partial class ErrorViewModel : NavigatableViewModelBase
{

    [ObservableProperty]
    private Exception ex;
    [ObservableProperty]
    private bool logVisible;

    public ICommand QuitCommand { get; set; }
    public ICommand CopyCommand { get; set; }
    public ICommand LogCommand { get; set; }
    public ICommand ContinueCommand { get; set; }
    public ICommand SettingsCommand { get; set; }

    public ErrorViewModel() : this(new NotImplementedException())
    {

    }

    public ErrorViewModel(Exception ex)
    {
        Ex = ex;
        LogVisible = false;

        QuitCommand = new RelayCommand(QuitAction);
        CopyCommand = new RelayCommand(CopyAction);
        LogCommand = new RelayCommand(QuitAction);
        ContinueCommand = new RelayCommand(ContinueAction);
        SettingsCommand = new RelayCommand(SettingsAction);
    }

    public async void CopyAction()
    {
        var clipboard = TopLevel.GetTopLevel(GetUserControl!())?.Clipboard;

        if (clipboard == null)
            return;

        await clipboard.SetTextAsync(Ex.ToString());
    }

    public void SettingsAction()
    {
        this.Navigate(new SettingsViewModel());
    }

    public void ContinueAction()
    {
        this.Navigate(new MainViewModel());
    }

    public void QuitAction()
    {
        Environment.Exit(0);
    }
}