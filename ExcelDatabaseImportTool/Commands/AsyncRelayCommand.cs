using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.Commands
{
    /// <summary>
    /// An asynchronous command implementation that relays its functionality to async delegates
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of AsyncRelayCommand
        /// </summary>
        /// <param name="execute">The async function to execute when the command is invoked</param>
        /// <param name="canExecute">The function to determine if the command can execute</param>
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Gets a value indicating whether the command is currently executing
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
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
            return !IsExecuting && (_canExecute?.Invoke() ?? true);
        }

        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public async void Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        /// <summary>
        /// Executes the command asynchronously and returns the task
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ExecuteAsync()
        {
            if (CanExecute(null))
            {
                try
                {
                    IsExecuting = true;
                    await _execute();
                }
                finally
                {
                    IsExecuting = false;
                }
            }
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
    /// A generic asynchronous command implementation that relays its functionality to async delegates
    /// </summary>
    /// <typeparam name="T">The type of the command parameter</typeparam>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of AsyncRelayCommand with a parameter
        /// </summary>
        /// <param name="execute">The async function to execute when the command is invoked</param>
        /// <param name="canExecute">The function to determine if the command can execute</param>
        public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Gets a value indicating whether the command is currently executing
        /// </summary>
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                RaiseCanExecuteChanged();
            }
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
            return !IsExecuting && (_canExecute?.Invoke((T?)parameter) ?? true);
        }

        /// <summary>
        /// Executes the command asynchronously
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        public async void Execute(object? parameter)
        {
            await ExecuteAsync((T?)parameter);
        }

        /// <summary>
        /// Executes the command asynchronously and returns the task
        /// </summary>
        /// <param name="parameter">The command parameter</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task ExecuteAsync(T? parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    IsExecuting = true;
                    await _execute(parameter);
                }
                finally
                {
                    IsExecuting = false;
                }
            }
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