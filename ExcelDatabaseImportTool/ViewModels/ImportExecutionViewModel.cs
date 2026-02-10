using ExcelDatabaseImportTool.Commands;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.ViewModels
{
    /// <summary>
    /// ViewModel for import execution interface
    /// </summary>
    public class ImportExecutionViewModel : BaseViewModel
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IImportService _importService;
        private readonly IImportLogRepository _importLogRepository;
        
        private ObservableCollection<ImportConfiguration> _importConfigurations;
        private ImportConfiguration? _selectedImportConfiguration;
        private string _selectedExcelFilePath;
        private bool _isImporting;
        private bool _canCancelImport;
        private int _progressPercentage;
        private string _progressMessage;
        private string _statusMessage;
        private ImportResult? _lastImportResult;
        private CancellationTokenSource? _cancellationTokenSource;

        // Result display properties
        private int _totalRecords;
        private int _successfulRecords;
        private int _failedRecords;
        private ObservableCollection<string> _errorMessages;
        private bool _hasResults;

        /// <summary>
        /// Initializes a new instance of ImportExecutionViewModel
        /// </summary>
        /// <param name="configurationRepository">Repository for configuration data access</param>
        /// <param name="importService">Service for import operations</param>
        /// <param name="importLogRepository">Repository for import log data access</param>
        public ImportExecutionViewModel(
            IConfigurationRepository configurationRepository,
            IImportService importService,
            IImportLogRepository importLogRepository)
        {
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _importLogRepository = importLogRepository ?? throw new ArgumentNullException(nameof(importLogRepository));
            
            _importConfigurations = new ObservableCollection<ImportConfiguration>();
            _selectedExcelFilePath = string.Empty;
            _progressMessage = string.Empty;
            _statusMessage = string.Empty;
            _errorMessages = new ObservableCollection<string>();

            // Subscribe to import progress events
            _importService.ProgressUpdated += OnImportProgressUpdated;

            // Initialize commands
            LoadConfigurationsCommand = new AsyncRelayCommand(LoadConfigurationsAsync);
            SelectExcelFileCommand = new RelayCommand(SelectExcelFile);
            ExecuteImportCommand = new AsyncRelayCommand(ExecuteImportAsync, CanExecuteImport);
            CancelImportCommand = new RelayCommand(CancelImport, CanCancelImport);
            ClearResultsCommand = new RelayCommand(ClearResults, () => HasResults);

            // Load configurations on initialization
            _ = LoadConfigurationsAsync();
        }

        #region Properties

        /// <summary>
        /// Gets the collection of import configurations
        /// </summary>
        public ObservableCollection<ImportConfiguration> ImportConfigurations
        {
            get => _importConfigurations;
            set => SetProperty(ref _importConfigurations, value);
        }

        /// <summary>
        /// Gets or sets the selected import configuration
        /// </summary>
        public ImportConfiguration? SelectedImportConfiguration
        {
            get => _selectedImportConfiguration;
            set
            {
                if (SetProperty(ref _selectedImportConfiguration, value))
                {
                    ((AsyncRelayCommand)ExecuteImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected Excel file path
        /// </summary>
        public string SelectedExcelFilePath
        {
            get => _selectedExcelFilePath;
            set
            {
                if (SetProperty(ref _selectedExcelFilePath, value))
                {
                    ((AsyncRelayCommand)ExecuteImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an import is in progress
        /// </summary>
        public bool IsImporting
        {
            get => _isImporting;
            set
            {
                if (SetProperty(ref _isImporting, value))
                {
                    ((AsyncRelayCommand)ExecuteImportCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)CancelImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the import can be cancelled
        /// </summary>
        public bool CanCancelImport
        {
            get => _canCancelImport;
            set
            {
                if (SetProperty(ref _canCancelImport, value))
                {
                    ((RelayCommand)CancelImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        /// <summary>
        /// Gets or sets the progress message
        /// </summary>
        public string ProgressMessage
        {
            get => _progressMessage;
            set => SetProperty(ref _progressMessage, value);
        }

        /// <summary>
        /// Gets or sets the status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets or sets the total number of records processed
        /// </summary>
        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        /// <summary>
        /// Gets or sets the number of successfully imported records
        /// </summary>
        public int SuccessfulRecords
        {
            get => _successfulRecords;
            set => SetProperty(ref _successfulRecords, value);
        }

        /// <summary>
        /// Gets or sets the number of failed records
        /// </summary>
        public int FailedRecords
        {
            get => _failedRecords;
            set => SetProperty(ref _failedRecords, value);
        }

        /// <summary>
        /// Gets the collection of error messages from the import
        /// </summary>
        public ObservableCollection<string> ErrorMessages
        {
            get => _errorMessages;
            set => SetProperty(ref _errorMessages, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether results are available
        /// </summary>
        public bool HasResults
        {
            get => _hasResults;
            set
            {
                if (SetProperty(ref _hasResults, value))
                {
                    ((RelayCommand)ClearResultsCommand).RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to load all import configurations
        /// </summary>
        public ICommand LoadConfigurationsCommand { get; }

        /// <summary>
        /// Command to select an Excel file
        /// </summary>
        public ICommand SelectExcelFileCommand { get; }

        /// <summary>
        /// Command to execute the import operation
        /// </summary>
        public ICommand ExecuteImportCommand { get; }

        /// <summary>
        /// Command to cancel the import operation
        /// </summary>
        public ICommand CancelImportCommand { get; }

        /// <summary>
        /// Command to clear the results display
        /// </summary>
        public ICommand ClearResultsCommand { get; }

        #endregion

        #region Command Implementations

        private async Task LoadConfigurationsAsync()
        {
            try
            {
                var configs = await _configurationRepository.GetImportConfigurationsAsync();
                ImportConfigurations.Clear();
                foreach (var config in configs)
                {
                    ImportConfigurations.Add(config);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading configurations: {ex.Message}";
            }
        }

        private void SelectExcelFile()
        {
            try
            {
                // In a real WPF application, this would use Microsoft.Win32.OpenFileDialog
                // For now, we'll provide a placeholder implementation
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*",
                    Title = "Select Excel File to Import"
                };

                if (dialog.ShowDialog() == true)
                {
                    SelectedExcelFilePath = dialog.FileName;
                    StatusMessage = $"Selected file: {Path.GetFileName(SelectedExcelFilePath)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error selecting file: {ex.Message}";
            }
        }

        private bool CanExecuteImport()
        {
            return !IsImporting 
                && SelectedImportConfiguration != null 
                && !string.IsNullOrWhiteSpace(SelectedExcelFilePath)
                && File.Exists(SelectedExcelFilePath);
        }

        private async Task ExecuteImportAsync()
        {
            if (SelectedImportConfiguration == null || string.IsNullOrWhiteSpace(SelectedExcelFilePath))
                return;

            IsImporting = true;
            CanCancelImport = true;
            ProgressPercentage = 0;
            ProgressMessage = "Starting import...";
            StatusMessage = "Import in progress...";
            ClearResults();

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Execute the import
                var result = await _importService.ImportDataAsync(
                    SelectedImportConfiguration, 
                    SelectedExcelFilePath,
                    _cancellationTokenSource.Token);

                _lastImportResult = result;

                // Update result display
                TotalRecords = result.TotalRecords;
                SuccessfulRecords = result.SuccessfulRecords;
                FailedRecords = result.FailedRecords;

                ErrorMessages.Clear();
                foreach (var error in result.Errors)
                {
                    ErrorMessages.Add(error);
                }

                HasResults = true;

                // Update status message based on result
                if (result.Success)
                {
                    StatusMessage = $"Import completed successfully! {result.SuccessfulRecords} of {result.TotalRecords} records imported.";
                    ProgressMessage = "Import completed";
                    ProgressPercentage = 100;
                }
                else if (result.FailedRecords < result.TotalRecords)
                {
                    StatusMessage = $"Import completed with errors. {result.SuccessfulRecords} of {result.TotalRecords} records imported successfully.";
                    ProgressMessage = "Import completed with errors";
                    ProgressPercentage = 100;
                }
                else
                {
                    StatusMessage = $"Import failed. No records were imported.";
                    ProgressMessage = "Import failed";
                    ProgressPercentage = 0;
                }

                // Save the import log
                if (result.ImportLog != null)
                {
                    await _importLogRepository.SaveImportLogAsync(result.ImportLog);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Import was cancelled by user.";
                ProgressMessage = "Import cancelled";
                HasResults = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import failed with error: {ex.Message}";
                ProgressMessage = "Import failed";
                ErrorMessages.Clear();
                ErrorMessages.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    ErrorMessages.Add($"Inner exception: {ex.InnerException.Message}");
                }
                HasResults = true;
            }
            finally
            {
                IsImporting = false;
                CanCancelImport = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private bool CanCancelImport()
        {
            return IsImporting && CanCancelImport;
        }

        private void CancelImport()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                StatusMessage = "Cancelling import...";
                ProgressMessage = "Cancelling...";
            }
        }

        private void ClearResults()
        {
            TotalRecords = 0;
            SuccessfulRecords = 0;
            FailedRecords = 0;
            ErrorMessages.Clear();
            HasResults = false;
        }

        #endregion

        #region Event Handlers

        private void OnImportProgressUpdated(object? sender, ImportProgressEventArgs e)
        {
            // Update progress on the UI thread
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (e.TotalRecords > 0)
                {
                    ProgressPercentage = (int)((double)e.ProcessedRecords / e.TotalRecords * 100);
                }
                ProgressMessage = e.CurrentOperation;
                CanCancelImport = e.CanCancel;
            });
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup resources when the ViewModel is disposed
        /// </summary>
        public void Dispose()
        {
            if (_importService != null)
            {
                _importService.ProgressUpdated -= OnImportProgressUpdated;
            }

            _cancellationTokenSource?.Dispose();
        }

        #endregion
    }
}
