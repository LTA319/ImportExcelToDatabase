using System;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.Commands
{
    /// <summary>
    /// A command implementation that relays its functionality to delegates
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of RelayCommand
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked</param>
        /// <param name="canExecute">The function to determine if the command can execute</param>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <returns>True if this command can be executed; otherwise, false</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public void Execute(object? parameter)
        {
            _execute();
        }

        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// A generic command implementation that relays its functionality to delegates
    /// </summary>
    /// <typeparam name="T">The type of the command parameter</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of RelayCommand with a parameter
        /// </summary>
        /// <param name="execute">The action to execute when the command is invoked</param>
        /// <param name="canExecute">The function to determine if the command can execute</param>
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Determines whether the command can execute in its current state
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <returns>True if this command can be executed; otherwise, false</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        /// <summary>
        /// Executes the command
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public void Execute(object? parameter)
        {
            _execute((T?)parameter);
        }

        /// <summary>
        /// Raises the CanExecuteChanged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}