using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using ImmichFrame.Exceptions;
using ImmichFrame.Helpers;
using ImmichFrame.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        private ObservableCollection<ListItem> excludedAlbumList;

        [ObservableProperty]
        public Settings settings;
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand QuitCommand { get; set; }
        public ICommand BackupCommand { get; set; }
        public ICommand RestoreCommand { get; set; }
        public ICommand AddPersonCommand { get; set; }
        public ICommand RemovePersonCommand { get; set; }
        public ICommand AddAlbumCommand { get; set; }
        public ICommand RemoveAlbumCommand { get; set; }
        public ICommand AddExcludedAlbumCommand { get; set; }
        public ICommand RemoveExcludedAlbumCommand { get; set; }
        public ICommand TestMarginCommand { get; set; }
        public List<string> StretchOptions { get; } = Enum.GetNames(typeof(Stretch)).ToList();

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
            BackupCommand = new RelayCommand(BackupAction);
            RestoreCommand = new RelayCommand(RestoreAction);
            AddPersonCommand = new RelayCommand(AddPersonAction);
            RemovePersonCommand = new RelayCommandParams(RemovePersonAction);
            AddAlbumCommand = new RelayCommand(AddAlbumAction);
            RemoveAlbumCommand = new RelayCommandParams(RemoveAlbumAction);
            AddExcludedAlbumCommand = new RelayCommand(AddExcludedAlbumAction);
            RemoveExcludedAlbumCommand = new RelayCommandParams(RemoveExcludedAlbumAction);
            TestMarginCommand = new RelayCommand(TestMarginAction);

            PeopleList = new ObservableCollection<ListItem>(Settings.People.Select(x => new ListItem(x.ToString())));
            AlbumList = new ObservableCollection<ListItem>(Settings.Albums.Select(x => new ListItem(x.ToString())));
            ExcludedAlbumList = new ObservableCollection<ListItem>(Settings.ExcludedAlbums.Select(x => new ListItem(x.ToString())));
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
        }

        private void AddAlbumAction()
        {
            AlbumList.Add(new ListItem());
        }

        private void RemoveAlbumAction(object param)
        {
            var item = AlbumList.First(x => x.Id == Guid.Parse(param.ToString()!));
            AlbumList?.Remove(item);
        }

        private void AddExcludedAlbumAction()
        {
            ExcludedAlbumList.Add(new ListItem());
        }

        private void RemoveExcludedAlbumAction(object param)
        {
            var item = ExcludedAlbumList.First(x => x.Id == Guid.Parse(param.ToString()!));
            ExcludedAlbumList?.Remove(item);
        }

        private void CancelAction()
        {
            try
            {
                Settings.ReloadFromJson();
                Navigate(new MainViewModel());
            }
            catch (SettingsNotValidException)
            {
                this.Navigate(new ErrorViewModel(new Exception("Please provide valid settings")));
            }
        }

        private void SaveAction()
        {
            // TODO: Validation
            try
            {
                Settings.People = PeopleList.Select(x => Guid.Parse(x.Value)).ToList();
                Settings.Albums = AlbumList.Select(x => Guid.Parse(x.Value)).ToList();
                Settings.ExcludedAlbums = ExcludedAlbumList.Select(x => Guid.Parse(x.Value)).ToList();
                if (string.IsNullOrEmpty(Settings.ImmichServerUrl) || string.IsNullOrEmpty(Settings.ApiKey))
                {
                    return;
                }
                Settings.SaveSettings(Settings);
                var settings = Settings.CurrentSettings;
            }
            catch (Exception ex)
            {
                // could not parse 
                this.Navigate(new ErrorViewModel(ex));
                return;
            }

            Navigate(new MainViewModel());
        }
        private void QuitAction()
        {
            Environment.Exit(0);
        }
        private async void BackupAction()
        {
            var backupFile = await ShowSaveFileDialog(true);
            if (backupFile is not null)
            {
                await Settings.BackupSettings(backupFile);
            }
        }
        private async void RestoreAction()
        {
            var restoreFile = await ShowOpenFileDialog();
            if (restoreFile is not null)
            {
                await Settings.RestoreSettings(restoreFile);
                Settings = Settings.CurrentSettings;
            }
        }
        public async Task<IStorageFile?> ShowSaveFileDialog(bool showOverwritePrompt)
        {
            var topLevel = TopLevel.GetTopLevel(GetUserControl!());
            if (topLevel != null)
            {
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save File",
                    SuggestedFileName = "ImmichFrameSettings.json",
                    ShowOverwritePrompt = showOverwritePrompt
                });
                if (file is not null)
                {
                    return file;
                }
            }
            return null;
        }
        public async Task<IStorageFile?> ShowOpenFileDialog()
        {
            var topLevel = TopLevel.GetTopLevel(GetUserControl!());
            if (topLevel != null)
            {
                var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open File",
                    AllowMultiple = false,
                    SuggestedFileName = "ImmichFrameSettings.json",
                });
                if (file is not null)
                {
                    return file[0];
                }
            }
            return null;
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
