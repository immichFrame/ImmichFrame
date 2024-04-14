using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Threading.Tasks;
using ImmichFrame.ViewModels;

namespace ImmichFrame.Views
{
    public class BaseView : UserControl
    {
        public BaseView()
        {
            Loaded += (sender, args) =>
            {
                var vm = (ViewModelBase)DataContext!;

                vm.ShowMessageBoxFromThread = this.ShowMessageBoxFromThread;
                vm.ShowMessageBox = this.ShowMessageBox;
            };
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
        }
    }
}
