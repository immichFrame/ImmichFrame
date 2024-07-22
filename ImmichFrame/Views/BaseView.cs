using Avalonia.Controls;
using ImmichFrame.ViewModels;
using System;

namespace ImmichFrame.Views
{
    public class BaseView : UserControl, IDisposable
    {
        public BaseView()
        {
            Loaded += (sender, args) =>
            {
                var vm = (ViewModelBase)DataContext!;

                if (vm is NavigatableViewModelBase navVm)
                    navVm.Disposed += Dispose;

                vm.GetUserControl = this.GetUserControl;
            };
        }

        public UserControl GetUserControl()
        {
            return this;
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
