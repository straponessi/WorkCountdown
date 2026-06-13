using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace WorkCountdown.Infrastructure
{
    public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
        public void Execute(object? _) => execute();
        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? p) => canExecute?.Invoke((T?)p) ?? true;
        public void Execute(object? p) => execute((T?)p);
        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
