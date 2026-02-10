using ExcelDatabaseImportTool.ViewModels;

namespace ExcelDatabaseImportTool.Services.Navigation
{
    /// <summary>
    /// Service for managing navigation between different views in the application
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Gets the current active ViewModel
        /// </summary>
        BaseViewModel? CurrentViewModel { get; }

        /// <summary>
        /// Navigates to the specified ViewModel
        /// </summary>
        /// <typeparam name="T">Type of ViewModel to navigate to</typeparam>
        void NavigateTo<T>() where T : BaseViewModel;

        /// <summary>
        /// Navigates to the specified ViewModel instance
        /// </summary>
        /// <param name="viewModel">The ViewModel instance to navigate to</param>
        void NavigateTo(BaseViewModel viewModel);

        /// <summary>
        /// Event raised when navigation occurs
        /// </summary>
        event System.Action<BaseViewModel?>? NavigationChanged;
    }
}