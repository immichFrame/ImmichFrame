using System;
using System.Windows.Input;

namespace ImmichFrame.Helpers
{
    public class RelayCommand : ICommand
    {
        private readonly Action _action;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _action();

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public RelayCommand(Action action)
        {
            _action = action;
        }
    }
    public class RelayCommandParams : ICommand
    {
        private readonly Action<object> _action;
        public RelayCommandParams(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public void Execute(object? parameter)
        {
            if (parameter == null)
                return;

            _action(parameter);
        }
    }
}
