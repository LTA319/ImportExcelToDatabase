using ExcelDatabaseImportTool.Commands;
using ExcelDatabaseImportTool.Services.Navigation;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.ViewModels
{
    /// <summary>
    /// Main window ViewModel that orchestrates navigation between different views
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private BaseViewModel? _currentViewModel;

        /// <summary>
        /// Initializes a new instance of MainWindowViewModel
        /// </summary>
        /// <param name="navigationService">Navigation service for managing view transitions</param>
        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new System.ArgumentNullException(nameof(navigationService));
            
            // Subscribe to navigation changes
            _navigationService.NavigationChanged += OnNavigationChanged;
            
            // Initialize commands
            NavigateToDatabaseConfigurationCommand = new RelayCommand(NavigateToDatabaseConfiguration);
            NavigateToImportConfigurationCommand = new RelayCommand(NavigateToImportConfiguration);
            NavigateToImportExecutionCommand = new RelayCommand(NavigateToImportExecution);
        }

        /// <summary>
        /// Gets or sets the current active ViewModel
        /// </summary>
        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        /// <summary>
        /// Command to navigate to database configuration view
        /// </summary>
        public ICommand NavigateToDatabaseConfigurationCommand { get; }

        /// <summary>
        /// Command to navigate to import configuration view
        /// </summary>
        public ICommand NavigateToImportConfigurationCommand { get; }

        /// <summary>
        /// Command to navigate to import execution view
        /// </summary>
        public ICommand NavigateToImportExecutionCommand { get; }

        private void NavigateToDatabaseConfiguration()
        {
            _navigationService.NavigateTo<DatabaseConfigurationViewModel>();
        }

        private void NavigateToImportConfiguration()
        {
            _navigationService.NavigateTo<ImportConfigurationViewModel>();
        }

        private void NavigateToImportExecution()
        {
            _navigationService.NavigateTo<ImportExecutionViewModel>();
        }

        private void OnNavigationChanged(BaseViewModel? viewModel)
        {
            CurrentViewModel = viewModel;
        }
    }
}