using ExcelDatabaseImportTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ExcelDatabaseImportTool.Services.Navigation
{
    /// <summary>
    /// Implementation of navigation service for managing view transitions
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private BaseViewModel? _currentViewModel;

        /// <summary>
        /// Initializes a new instance of NavigationService
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving ViewModels</param>
        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the current active ViewModel
        /// </summary>
        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                _currentViewModel = value;
                NavigationChanged?.Invoke(_currentViewModel);
            }
        }

        /// <summary>
        /// Event raised when navigation occurs
        /// </summary>
        public event Action<BaseViewModel?>? NavigationChanged;

        /// <summary>
        /// Navigates to the specified ViewModel type
        /// </summary>
        /// <typeparam name="T">Type of ViewModel to navigate to</typeparam>
        public void NavigateTo<T>() where T : BaseViewModel
        {
            var viewModel = _serviceProvider.GetRequiredService<T>();
            NavigateTo(viewModel);
        }

        /// <summary>
        /// Navigates to the specified ViewModel instance
        /// </summary>
        /// <param name="viewModel">The ViewModel instance to navigate to</param>
        public void NavigateTo(BaseViewModel viewModel)
        {
            CurrentViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}