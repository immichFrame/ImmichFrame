using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ImmichFrame.ViewModels
{
    internal partial class WelcomeViewModel : NavigatableViewModelBase
    {
        public ICommand SettingsCommand { get; set; }
        public ICommand ExitCommand { get; set; }

        [ObservableProperty]
        private ObservableCollection<LinkItem> linkList;

        public WelcomeViewModel()
        {
            SettingsCommand = new RelayCommand(SettingsAction);
            ExitCommand = new RelayCommand(ExitAction);
            LinkList = new ObservableCollection<LinkItem>
            {
                new LinkItem()
                {
                    Text = "Github:",
                    Link = "https://github.com/3rob3/ImmichFrame"
                },
                new LinkItem()
                {
                    Text = "Support (Discord):",
                    Link = "https://discord.com/channels/979116623879368755/1217843270244372480"
                }
            };
        }

        public void SettingsAction()
        {
            this.Navigate(new SettingsViewModel(false));
        }

        public void ExitAction()
        {
            Environment.Exit(0);
        }
    }

    internal class LinkItem
    {
        public string Text { get; set; }
        public string Link { get; set; }
    }
}
