using ExcelDatabaseImportTool.Commands;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.ViewModels
{
    /// <summary>
    /// ViewModel for database configuration management
    /// </summary>
    public class DatabaseConfigurationViewModel : BaseViewModel
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IDatabaseConnectionService _connectionService;
        private readonly IEncryptionService _encryptionService;
        
        private ObservableCollection<DatabaseConfiguration> _configurations;
        private DatabaseConfiguration? _selectedConfiguration;
        private DatabaseConfiguration _currentConfiguration;
        private bool _isEditing;
        private bool _isTestingConnection;
        private string _connectionTestResult;
        private string _validationErrors;
        private string _plainTextPassword;

        /// <summary>
        /// Initializes a new instance of DatabaseConfigurationViewModel
        /// </summary>
        /// <param name="configurationRepository">Repository for configuration data access</param>
        /// <param name="connectionService">Service for database connection operations</param>
        /// <param name="encryptionService">Service for password encryption</param>
        public DatabaseConfigurationViewModel(
            IConfigurationRepository configurationRepository,
            IDatabaseConnectionService connectionService,
            IEncryptionService encryptionService)
        {
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
            
            _configurations = new ObservableCollection<DatabaseConfiguration>();
            _currentConfiguration = new DatabaseConfiguration();
            _connectionTestResult = string.Empty;
            _validationErrors = string.Empty;
            _plainTextPassword = string.Empty;

            // Initialize commands
            LoadConfigurationsCommand = new AsyncRelayCommand(LoadConfigurationsAsync);
            AddConfigurationCommand = new RelayCommand(AddConfiguration);
            EditConfigurationCommand = new RelayCommand<DatabaseConfiguration>(EditConfiguration, CanEditConfiguration);
            DeleteConfigurationCommand = new AsyncRelayCommand<DatabaseConfiguration>(DeleteConfigurationAsync, CanDeleteConfiguration);
            SaveConfigurationCommand = new AsyncRelayCommand(SaveConfigurationAsync, CanSaveConfiguration);
            CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditing);
            TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, CanTestConnection);

            // Load configurations on initialization
            _ = LoadConfigurationsAsync();
        }

        #region Properties

        /// <summary>
        /// Gets the available database types for the ComboBox
        /// </summary>
        public IEnumerable<DatabaseType> DatabaseTypes => Enum.GetValues(typeof(DatabaseType)).Cast<DatabaseType>();

        /// <summary>
        /// Gets or sets the plain text password (not persisted)
        /// </summary>
        public string PlainTextPassword
        {
            get => _plainTextPassword;
            set => SetProperty(ref _plainTextPassword, value);
        }

        /// <summary>
        /// Gets the collection of database configurations
        /// </summary>
        public ObservableCollection<DatabaseConfiguration> Configurations
        {
            get => _configurations;
            set => SetProperty(ref _configurations, value);
        }

        /// <summary>
        /// Gets or sets the selected configuration in the list
        /// </summary>
        public DatabaseConfiguration? SelectedConfiguration
        {
            get => _selectedConfiguration;
            set => SetProperty(ref _selectedConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the current configuration being edited
        /// </summary>
        public DatabaseConfiguration CurrentConfiguration
        {
            get => _currentConfiguration;
            set => SetProperty(ref _currentConfiguration, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the form is in editing mode
        /// </summary>
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a connection test is in progress
        /// </summary>
        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            set => SetProperty(ref _isTestingConnection, value);
        }

        /// <summary>
        /// Gets or sets the connection test result message
        /// </summary>
        public string ConnectionTestResult
        {
            get => _connectionTestResult;
            set => SetProperty(ref _connectionTestResult, value);
        }

        /// <summary>
        /// Gets or sets validation error messages
        /// </summary>
        public string ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to load all database configurations
        /// </summary>
        public ICommand LoadConfigurationsCommand { get; }

        /// <summary>
        /// Command to add a new database configuration
        /// </summary>
        public ICommand AddConfigurationCommand { get; }

        /// <summary>
        /// Command to edit an existing database configuration
        /// </summary>
        public ICommand EditConfigurationCommand { get; }

        /// <summary>
        /// Command to delete a database configuration
        /// </summary>
        public ICommand DeleteConfigurationCommand { get; }

        /// <summary>
        /// Command to save the current configuration
        /// </summary>
        public ICommand SaveConfigurationCommand { get; }

        /// <summary>
        /// Command to cancel editing
        /// </summary>
        public ICommand CancelEditCommand { get; }

        /// <summary>
        /// Command to test database connection
        /// </summary>
        public ICommand TestConnectionCommand { get; }

        #endregion

        #region Command Implementations

        private async Task LoadConfigurationsAsync()
        {
            try
            {
                var configs = await _configurationRepository.GetDatabaseConfigurationsAsync();
                Configurations.Clear();
                foreach (var config in configs)
                {
                    Configurations.Add(config);
                }
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error loading configurations: {ex.Message}";
            }
        }

        private void AddConfiguration()
        {
            CurrentConfiguration = new DatabaseConfiguration
            {
                Type = DatabaseType.MySQL,
                Port = 3306,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };
            PlainTextPassword = string.Empty;
            IsEditing = true;
            ValidationErrors = string.Empty;
            ConnectionTestResult = string.Empty;
        }

        private bool CanEditConfiguration(DatabaseConfiguration? config)
        {
            return config != null && !IsEditing;
        }

        private void EditConfiguration(DatabaseConfiguration? config)
        {
            if (config != null)
            {
                CurrentConfiguration = new DatabaseConfiguration
                {
                    Id = config.Id,
                    Name = config.Name,
                    Type = config.Type,
                    Server = config.Server,
                    Database = config.Database,
                    Username = config.Username,
                    EncryptedPassword = config.EncryptedPassword,
                    Port = config.Port,
                    ConnectionString = config.ConnectionString,
                    CreatedDate = config.CreatedDate,
                    ModifiedDate = DateTime.Now
                };
                PlainTextPassword = string.Empty; // Don't show existing password
                IsEditing = true;
                ValidationErrors = string.Empty;
                ConnectionTestResult = string.Empty;
            }
        }

        private bool CanDeleteConfiguration(DatabaseConfiguration? config)
        {
            return config != null && !IsEditing;
        }

        private async Task DeleteConfigurationAsync(DatabaseConfiguration? config)
        {
            if (config == null) return;

            try
            {
                // Check if configuration is referenced by import configurations
                var isReferenced = await _configurationRepository.IsDatabaseConfigurationReferencedAsync(config.Id);
                if (isReferenced)
                {
                    ValidationErrors = "Cannot delete configuration: it is referenced by existing import configurations.";
                    return;
                }

                await _configurationRepository.DeleteDatabaseConfigurationAsync(config.Id);
                Configurations.Remove(config);
                ValidationErrors = string.Empty;
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error deleting configuration: {ex.Message}";
            }
        }

        private bool CanSaveConfiguration()
        {
            return IsEditing && !IsTestingConnection;
        }

        private async Task SaveConfigurationAsync()
        {
            try
            {
                // Encrypt the password if a new one was entered
                if (!string.IsNullOrEmpty(PlainTextPassword))
                {
                    CurrentConfiguration.EncryptedPassword = _encryptionService.Encrypt(PlainTextPassword);
                }

                // Validate the configuration
                var validationResults = ValidateConfiguration(CurrentConfiguration);
                if (validationResults.Any())
                {
                    ValidationErrors = string.Join(Environment.NewLine, validationResults);
                    return;
                }

                // Build connection string
                CurrentConfiguration.ConnectionString = _connectionService.BuildConnectionString(CurrentConfiguration);
                CurrentConfiguration.ModifiedDate = DateTime.Now;

                if (CurrentConfiguration.Id == 0)
                {
                    CurrentConfiguration.CreatedDate = DateTime.Now;
                }

                await _configurationRepository.SaveDatabaseConfigurationAsync(CurrentConfiguration);

                // Refresh the list
                await LoadConfigurationsAsync();

                IsEditing = false;
                PlainTextPassword = string.Empty;
                ValidationErrors = string.Empty;
                ConnectionTestResult = string.Empty;
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error saving configuration: {ex.Message}";
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            CurrentConfiguration = new DatabaseConfiguration();
            PlainTextPassword = string.Empty;
            ValidationErrors = string.Empty;
            ConnectionTestResult = string.Empty;
        }

        private bool CanTestConnection()
        {
            return IsEditing && !IsTestingConnection && !string.IsNullOrWhiteSpace(CurrentConfiguration.Server);
        }

        private async Task TestConnectionAsync()
        {
            IsTestingConnection = true;
            ConnectionTestResult = "Testing connection...";

            try
            {
                // Create a temporary configuration with encrypted password for testing
                var testConfig = new DatabaseConfiguration
                {
                    Type = CurrentConfiguration.Type,
                    Server = CurrentConfiguration.Server,
                    Database = CurrentConfiguration.Database,
                    Username = CurrentConfiguration.Username,
                    Port = CurrentConfiguration.Port,
                    EncryptedPassword = !string.IsNullOrEmpty(PlainTextPassword) 
                        ? _encryptionService.Encrypt(PlainTextPassword)
                        : CurrentConfiguration.EncryptedPassword
                };

                var success = await _connectionService.TestConnectionAsync(testConfig);
                ConnectionTestResult = success ? "Connection successful!" : "Connection failed.";
            }
            catch (Exception ex)
            {
                ConnectionTestResult = $"Connection failed: {ex.Message}";
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        #endregion

        #region Validation

        private List<string> ValidateConfiguration(DatabaseConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.Name))
                errors.Add("Configuration name is required.");

            if (string.IsNullOrWhiteSpace(config.Server))
                errors.Add("Server is required.");

            if (string.IsNullOrWhiteSpace(config.Database))
                errors.Add("Database name is required.");

            if (string.IsNullOrWhiteSpace(config.Username))
                errors.Add("Username is required.");

            // Check if password is provided (either new plain text or existing encrypted)
            if (string.IsNullOrWhiteSpace(PlainTextPassword) && string.IsNullOrWhiteSpace(config.EncryptedPassword))
                errors.Add("Password is required.");

            if (config.Port <= 0 || config.Port > 65535)
                errors.Add("Port must be between 1 and 65535.");

            // Check for duplicate names (excluding current configuration)
            var existingConfig = Configurations.FirstOrDefault(c => 
                c.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase) && c.Id != config.Id);
            if (existingConfig != null)
                errors.Add("A configuration with this name already exists.");

            return errors;
        }

        #endregion
    }
}