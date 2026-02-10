using ExcelDatabaseImportTool.Commands;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Models.Domain;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.ViewModels
{
    /// <summary>
    /// ViewModel for import history viewing
    /// </summary>
    public class ImportHistoryViewModel : BaseViewModel
    {
        private readonly IImportLogRepository _importLogRepository;
        
        private ObservableCollection<ImportLog> _importLogs;
        private ImportLog? _selectedImportLog;
        private DateTime? _filterFromDate;
        private DateTime? _filterToDate;
        private string _filterStatus;
        private string _searchText;
        private bool _isLoading;

        /// <summary>
        /// Initializes a new instance of ImportHistoryViewModel
        /// </summary>
        /// <param name="importLogRepository">Repository for import log data access</param>
        public ImportHistoryViewModel(IImportLogRepository importLogRepository)
        {
            _importLogRepository = importLogRepository ?? throw new ArgumentNullException(nameof(importLogRepository));
            
            _importLogs = new ObservableCollection<ImportLog>();
            _filterStatus = "All";
            _searchText = string.Empty;

            // Initialize commands
            LoadLogsCommand = new AsyncRelayCommand(LoadLogsAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshLogsAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ApplyFiltersCommand = new AsyncRelayCommand(ApplyFiltersAsync);

            // Load logs on initialization
            _ = LoadLogsAsync();
        }

        #region Properties

        /// <summary>
        /// Gets the collection of import logs
        /// </summary>
        public ObservableCollection<ImportLog> ImportLogs
        {
            get => _importLogs;
            set => SetProperty(ref _importLogs, value);
        }

        /// <summary>
        /// Gets or sets the selected import log
        /// </summary>
        public ImportLog? SelectedImportLog
        {
            get => _selectedImportLog;
            set => SetProperty(ref _selectedImportLog, value);
        }

        /// <summary>
        /// Gets or sets the filter start date
        /// </summary>
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set => SetProperty(ref _filterFromDate, value);
        }

        /// <summary>
        /// Gets or sets the filter end date
        /// </summary>
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set => SetProperty(ref _filterToDate, value);
        }

        /// <summary>
        /// Gets or sets the filter status
        /// </summary>
        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        /// <summary>
        /// Gets or sets the search text
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether logs are being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to load import logs
        /// </summary>
        public ICommand LoadLogsCommand { get; }

        /// <summary>
        /// Command to refresh import logs
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to clear all filters
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>
        /// Command to apply filters
        /// </summary>
        public ICommand ApplyFiltersCommand { get; }

        #endregion

        #region Command Implementations

        private async Task LoadLogsAsync()
        {
            IsLoading = true;
            try
            {
                var logs = await _importLogRepository.GetImportLogsAsync(FilterFromDate);
                ImportLogs.Clear();
                
                // Apply filters
                var filteredLogs = ApplyLocalFilters(logs);
                
                foreach (var log in filteredLogs.OrderByDescending(l => l.StartTime))
                {
                    ImportLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                // In a real application, you would log this error
                System.Diagnostics.Debug.WriteLine($"Error loading import logs: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshLogsAsync()
        {
            await LoadLogsAsync();
        }

        private void ClearFilters()
        {
            FilterFromDate = null;
            FilterToDate = null;
            FilterStatus = "All";
            SearchText = string.Empty;
            _ = LoadLogsAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            await LoadLogsAsync();
        }

        #endregion

        #region Helper Methods

        private List<ImportLog> ApplyLocalFilters(List<ImportLog> logs)
        {
            var filtered = logs.AsEnumerable();

            // Filter by date range
            if (FilterFromDate.HasValue)
            {
                filtered = filtered.Where(l => l.StartTime >= FilterFromDate.Value);
            }

            if (FilterToDate.HasValue)
            {
                filtered = filtered.Where(l => l.StartTime <= FilterToDate.Value.AddDays(1));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(FilterStatus) && FilterStatus != "All")
            {
                if (Enum.TryParse<ImportStatus>(FilterStatus, out var status))
                {
                    filtered = filtered.Where(l => l.Status == status);
                }
            }

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(l => 
                    (l.ExcelFileName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (l.ErrorDetails?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return filtered.ToList();
        }

        #endregion
    }
}
