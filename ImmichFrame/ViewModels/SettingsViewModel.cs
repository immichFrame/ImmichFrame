using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ImmichFrame.ViewModels
{
    public partial class SettingsViewModel : NavigatableViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ListItem> peopleList;

        [ObservableProperty]
        private ObservableCollection<ListItem> albumList;

        [ObservableProperty]
        public Settings settings;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand QuitCommand { get; set; }
        public ICommand AddPersonCommand { get; set; }
        public ICommand RemovePersonCommand { get; set; }
        public ICommand AddAlbumCommand { get; set; }
        public ICommand RemoveAlbumCommand { get; set; }
        public ICommand TestMarginCommand { get; set; }
        public event EventHandler? ListItemRemoved;

        public SettingsViewModel()
        {
            try
            {
                Settings = Settings.CurrentSettings;
            }
            catch (SettingsNotValidException)
            {
                Settings = new Settings();
            }

            SaveCommand = new RelayCommand(SaveAction);
            CancelCommand = new RelayCommand(CancelAction);
            QuitCommand = new RelayCommand(QuitAction);
            AddPersonCommand = new RelayCommand(AddPersonAction);
            RemovePersonCommand = new RelayCommandParams(RemovePersonAction);
            AddAlbumCommand = new RelayCommand(AddAlbumAction);
            RemoveAlbumCommand = new RelayCommandParams(RemoveAlbumAction);
            TestMarginCommand = new RelayCommand(TestMarginAction);

            PeopleList = new ObservableCollection<ListItem>(Settings.People.Select(x => new ListItem(x.ToString())));
            AlbumList = new ObservableCollection<ListItem>(Settings.Albums.Select(x => new ListItem(x.ToString())));
        }

        private void TestMarginAction()
        {
            this.UpdateMargin(Settings.Margin);
        }

        private void AddPersonAction()
        {
            PeopleList.Add(new ListItem());
        }

        private void RemovePersonAction(object param)
        {
            var item = PeopleList.First(x => x.Id == Guid.Parse(param.ToString()!));
            PeopleList?.Remove(item);
            ListItemRemoved?.Invoke(this, EventArgs.Empty);
        }

        private void AddAlbumAction()
        {
            AlbumList.Add(new ListItem());
        }

        private void RemoveAlbumAction(object param)
        {
            var item = AlbumList.First(x => x.Id == Guid.Parse(param.ToString()!));
            AlbumList?.Remove(item);
            ListItemRemoved?.Invoke(this, EventArgs.Empty);
        }

        private void CancelAction()
        {
            try
            {
                var settings = Settings.CurrentSettings;
                Navigate(new MainViewModel());
            }
            catch (SettingsNotValidException)
            {
                ShowMessageBox!("Please provide valid settings", "Invalid Settings");
            }
        }

        private void SaveAction()
        {
            // TODO: Validation
            try
            {
                Settings.People = PeopleList.Select(x => Guid.Parse(x.Value)).ToList();
                Settings.Albums = AlbumList.Select(x => Guid.Parse(x.Value)).ToList();

                Settings.SaveSettings(Settings);
                var settings = Settings.CurrentSettings;
            }
            catch (Exception ex)
            {
                // could not parse 
                ShowMessageBox!(ex.Message, "Error");
                return;
            }

            Navigate(new MainViewModel());
        }
        private void QuitAction()
        {
            Environment.Exit(0);
        }
    }

    public class ListItem
    {
        public ListItem(string value = "")
        {
            Id = Guid.NewGuid();
            Value = value;
        }

        public Guid Id { get; set; }
        public string Value { get; set; }
    }
}
