using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Threading.Tasks;
using ImmichFrame.ViewModels;
using System;

namespace ImmichFrame.Views
{
    public class BaseView : UserControl, IDisposable
    {
        public event EventHandler? MessageBoxClosed;
        public BaseView()
        {
            Loaded += (sender, args) =>
            {
                var vm = (ViewModelBase)DataContext!;

                if (vm is NavigatableViewModelBase navVm)
                    navVm.Disposed += Dispose;

                vm.ShowMessageBoxFromThread = this.ShowMessageBoxFromThread;
                vm.ShowMessageBox = this.ShowMessageBox;
                vm.GetUserControl = this.GetUserControl;
            };
        }

        public UserControl GetUserControl()
        {
            return this;
        }

        public Task ShowMessageBoxFromThread(string message, string title = "")
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.UIThread.Post(async () =>
            {
                await ShowMessageBox(message, title);
                tcs.SetResult(true);
            });
            return tcs.Task;
        }

        public async Task ShowMessageBox(string message, string title = "")
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowAsPopupAsync(this);
            MessageBoxClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose(object? sender, EventArgs? e)
        {
            Dispose();
        }


        public virtual void Dispose()
        {

        }
    }
}
