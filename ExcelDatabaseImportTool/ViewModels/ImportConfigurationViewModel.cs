using ExcelDatabaseImportTool.Commands;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ExcelDatabaseImportTool.ViewModels
{
    /// <summary>
    /// ViewModel for import configuration management
    /// </summary>
    public class ImportConfigurationViewModel : BaseViewModel
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IExcelReaderService _excelReaderService;
        
        private ObservableCollection<ImportConfiguration> _importConfigurations;
        private ObservableCollection<DatabaseConfiguration> _databaseConfigurations;
        private ObservableCollection<FieldMapping> _fieldMappings;
        private ImportConfiguration? _selectedImportConfiguration;
        private ImportConfiguration _currentImportConfiguration;
        private FieldMapping? _selectedFieldMapping;
        private FieldMapping _currentFieldMapping;
        private ForeignKeyMapping _currentForeignKeyMapping;
        private bool _isEditing;
        private bool _isEditingFieldMapping;
        private bool _isConfiguringForeignKey;
        private string _validationErrors;
        private string _selectedExcelFilePath;
        private ObservableCollection<string> _availableExcelColumns;

        /// <summary>
        /// Initializes a new instance of ImportConfigurationViewModel
        /// </summary>
        /// <param name="configurationRepository">Repository for configuration data access</param>
        /// <param name="excelReaderService">Service for reading Excel files</param>
        public ImportConfigurationViewModel(
            IConfigurationRepository configurationRepository,
            IExcelReaderService excelReaderService)
        {
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _excelReaderService = excelReaderService ?? throw new ArgumentNullException(nameof(excelReaderService));
            
            _importConfigurations = new ObservableCollection<ImportConfiguration>();
            _databaseConfigurations = new ObservableCollection<DatabaseConfiguration>();
            _fieldMappings = new ObservableCollection<FieldMapping>();
            _availableExcelColumns = new ObservableCollection<string>();
            _currentImportConfiguration = new ImportConfiguration();
            _currentFieldMapping = new FieldMapping();
            _currentForeignKeyMapping = new ForeignKeyMapping();
            _validationErrors = string.Empty;
            _selectedExcelFilePath = string.Empty;

            // Initialize commands
            LoadConfigurationsCommand = new AsyncRelayCommand(LoadConfigurationsAsync);
            AddImportConfigurationCommand = new RelayCommand(AddImportConfiguration);
            EditImportConfigurationCommand = new RelayCommand<ImportConfiguration>(EditImportConfiguration, CanEditImportConfiguration);
            DeleteImportConfigurationCommand = new AsyncRelayCommand<ImportConfiguration>(DeleteImportConfigurationAsync, CanDeleteImportConfiguration);
            SaveImportConfigurationCommand = new AsyncRelayCommand(SaveImportConfigurationAsync, CanSaveImportConfiguration);
            CancelEditCommand = new RelayCommand(CancelEdit, () => IsEditing);
            
            // Field mapping commands
            AddFieldMappingCommand = new RelayCommand(AddFieldMapping, CanAddFieldMapping);
            EditFieldMappingCommand = new RelayCommand<FieldMapping>(EditFieldMapping, CanEditFieldMapping);
            DeleteFieldMappingCommand = new RelayCommand<FieldMapping>(DeleteFieldMapping, CanDeleteFieldMapping);
            SaveFieldMappingCommand = new RelayCommand(SaveFieldMapping, CanSaveFieldMapping);
            CancelFieldMappingEditCommand = new RelayCommand(CancelFieldMappingEdit, () => IsEditingFieldMapping);
            
            // Foreign key mapping commands
            ConfigureForeignKeyCommand = new RelayCommand(ConfigureForeignKey, CanConfigureForeignKey);
            SaveForeignKeyMappingCommand = new RelayCommand(SaveForeignKeyMapping, CanSaveForeignKeyMapping);
            RemoveForeignKeyMappingCommand = new RelayCommand(RemoveForeignKeyMapping, CanRemoveForeignKeyMapping);
            CancelForeignKeyConfigurationCommand = new RelayCommand(CancelForeignKeyConfiguration, () => IsConfiguringForeignKey);
            
            // Excel file commands
            SelectExcelFileCommand = new AsyncRelayCommand(SelectExcelFileAsync);
            LoadExcelColumnsCommand = new AsyncRelayCommand(LoadExcelColumnsAsync, CanLoadExcelColumns);

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
        /// Gets the collection of database configurations
        /// </summary>
        public ObservableCollection<DatabaseConfiguration> DatabaseConfigurations
        {
            get => _databaseConfigurations;
            set => SetProperty(ref _databaseConfigurations, value);
        }

        /// <summary>
        /// Gets the collection of field mappings for the current import configuration
        /// </summary>
        public ObservableCollection<FieldMapping> FieldMappings
        {
            get => _fieldMappings;
            set => SetProperty(ref _fieldMappings, value);
        }

        /// <summary>
        /// Gets or sets the selected import configuration in the list
        /// </summary>
        public ImportConfiguration? SelectedImportConfiguration
        {
            get => _selectedImportConfiguration;
            set
            {
                if (SetProperty(ref _selectedImportConfiguration, value))
                {
                    LoadFieldMappings();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current import configuration being edited
        /// </summary>
        public ImportConfiguration CurrentImportConfiguration
        {
            get => _currentImportConfiguration;
            set => SetProperty(ref _currentImportConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the selected field mapping in the list
        /// </summary>
        public FieldMapping? SelectedFieldMapping
        {
            get => _selectedFieldMapping;
            set => SetProperty(ref _selectedFieldMapping, value);
        }

        /// <summary>
        /// Gets or sets the current field mapping being edited
        /// </summary>
        public FieldMapping CurrentFieldMapping
        {
            get => _currentFieldMapping;
            set => SetProperty(ref _currentFieldMapping, value);
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
        /// Gets or sets a value indicating whether the field mapping form is in editing mode
        /// </summary>
        public bool IsEditingFieldMapping
        {
            get => _isEditingFieldMapping;
            set => SetProperty(ref _isEditingFieldMapping, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the foreign key configuration form is active
        /// </summary>
        public bool IsConfiguringForeignKey
        {
            get => _isConfiguringForeignKey;
            set => SetProperty(ref _isConfiguringForeignKey, value);
        }

        /// <summary>
        /// Gets or sets the current foreign key mapping being configured
        /// </summary>
        public ForeignKeyMapping CurrentForeignKeyMapping
        {
            get => _currentForeignKeyMapping;
            set => SetProperty(ref _currentForeignKeyMapping, value);
        }

        /// <summary>
        /// Gets or sets validation error messages
        /// </summary>
        public string ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        /// <summary>
        /// Gets or sets the selected Excel file path
        /// </summary>
        public string SelectedExcelFilePath
        {
            get => _selectedExcelFilePath;
            set => SetProperty(ref _selectedExcelFilePath, value);
        }

        /// <summary>
        /// Gets the collection of available Excel columns
        /// </summary>
        public ObservableCollection<string> AvailableExcelColumns
        {
            get => _availableExcelColumns;
            set => SetProperty(ref _availableExcelColumns, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to load all import configurations
        /// </summary>
        public ICommand LoadConfigurationsCommand { get; }

        /// <summary>
        /// Command to add a new import configuration
        /// </summary>
        public ICommand AddImportConfigurationCommand { get; }

        /// <summary>
        /// Command to edit an existing import configuration
        /// </summary>
        public ICommand EditImportConfigurationCommand { get; }

        /// <summary>
        /// Command to delete an import configuration
        /// </summary>
        public ICommand DeleteImportConfigurationCommand { get; }

        /// <summary>
        /// Command to save the current import configuration
        /// </summary>
        public ICommand SaveImportConfigurationCommand { get; }

        /// <summary>
        /// Command to cancel editing
        /// </summary>
        public ICommand CancelEditCommand { get; }

        /// <summary>
        /// Command to add a new field mapping
        /// </summary>
        public ICommand AddFieldMappingCommand { get; }

        /// <summary>
        /// Command to edit an existing field mapping
        /// </summary>
        public ICommand EditFieldMappingCommand { get; }

        /// <summary>
        /// Command to delete a field mapping
        /// </summary>
        public ICommand DeleteFieldMappingCommand { get; }

        /// <summary>
        /// Command to save the current field mapping
        /// </summary>
        public ICommand SaveFieldMappingCommand { get; }

        /// <summary>
        /// Command to cancel field mapping editing
        /// </summary>
        public ICommand CancelFieldMappingEditCommand { get; }

        /// <summary>
        /// Command to select an Excel file
        /// </summary>
        public ICommand SelectExcelFileCommand { get; }

        /// <summary>
        /// Command to load Excel columns from the selected file
        /// </summary>
        public ICommand LoadExcelColumnsCommand { get; }

        /// <summary>
        /// Command to configure foreign key mapping for the current field mapping
        /// </summary>
        public ICommand ConfigureForeignKeyCommand { get; }

        /// <summary>
        /// Command to save the foreign key mapping
        /// </summary>
        public ICommand SaveForeignKeyMappingCommand { get; }

        /// <summary>
        /// Command to remove the foreign key mapping from the current field mapping
        /// </summary>
        public ICommand RemoveForeignKeyMappingCommand { get; }

        /// <summary>
        /// Command to cancel foreign key configuration
        /// </summary>
        public ICommand CancelForeignKeyConfigurationCommand { get; }

        #endregion

        #region Command Implementations

        private async Task LoadConfigurationsAsync()
        {
            try
            {
                var importConfigs = await _configurationRepository.GetImportConfigurationsAsync();
                ImportConfigurations.Clear();
                foreach (var config in importConfigs)
                {
                    ImportConfigurations.Add(config);
                }

                var dbConfigs = await _configurationRepository.GetDatabaseConfigurationsAsync();
                DatabaseConfigurations.Clear();
                foreach (var config in dbConfigs)
                {
                    DatabaseConfigurations.Add(config);
                }
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error loading configurations: {ex.Message}";
            }
        }

        private void AddImportConfiguration()
        {
            CurrentImportConfiguration = new ImportConfiguration
            {
                HasHeaderRow = true,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                FieldMappings = new List<FieldMapping>()
            };
            IsEditing = true;
            ValidationErrors = string.Empty;
            FieldMappings.Clear();
        }

        private bool CanEditImportConfiguration(ImportConfiguration? config)
        {
            return config != null && !IsEditing;
        }

        private void EditImportConfiguration(ImportConfiguration? config)
        {
            if (config != null)
            {
                CurrentImportConfiguration = new ImportConfiguration
                {
                    Id = config.Id,
                    Name = config.Name,
                    DatabaseConfigurationId = config.DatabaseConfigurationId,
                    TableName = config.TableName,
                    HasHeaderRow = config.HasHeaderRow,
                    CreatedDate = config.CreatedDate,
                    ModifiedDate = DateTime.Now,
                    FieldMappings = new List<FieldMapping>(config.FieldMappings)
                };
                IsEditing = true;
                ValidationErrors = string.Empty;
                LoadFieldMappings();
            }
        }

        private bool CanDeleteImportConfiguration(ImportConfiguration? config)
        {
            return config != null && !IsEditing;
        }

        private async Task DeleteImportConfigurationAsync(ImportConfiguration? config)
        {
            if (config == null) return;

            try
            {
                await _configurationRepository.DeleteImportConfigurationAsync(config.Id);
                ImportConfigurations.Remove(config);
                ValidationErrors = string.Empty;
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error deleting import configuration: {ex.Message}";
            }
        }

        private bool CanSaveImportConfiguration()
        {
            return IsEditing;
        }

        private async Task SaveImportConfigurationAsync()
        {
            try
            {
                // Validate the configuration
                var validationResults = ValidateImportConfiguration(CurrentImportConfiguration);
                if (validationResults.Any())
                {
                    ValidationErrors = string.Join(Environment.NewLine, validationResults);
                    return;
                }

                CurrentImportConfiguration.ModifiedDate = DateTime.Now;
                CurrentImportConfiguration.FieldMappings = FieldMappings.ToList();

                if (CurrentImportConfiguration.Id == 0)
                {
                    CurrentImportConfiguration.CreatedDate = DateTime.Now;
                }

                await _configurationRepository.SaveImportConfigurationAsync(CurrentImportConfiguration);

                // Refresh the list
                await LoadConfigurationsAsync();

                IsEditing = false;
                ValidationErrors = string.Empty;
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error saving import configuration: {ex.Message}";
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            CurrentImportConfiguration = new ImportConfiguration();
            ValidationErrors = string.Empty;
            FieldMappings.Clear();
        }

        private bool CanAddFieldMapping()
        {
            return IsEditing;
        }

        private void AddFieldMapping()
        {
            CurrentFieldMapping = new FieldMapping
            {
                DataType = "string"
            };
            IsEditingFieldMapping = true;
        }

        private bool CanEditFieldMapping(FieldMapping? mapping)
        {
            return mapping != null && IsEditing && !IsEditingFieldMapping;
        }

        private void EditFieldMapping(FieldMapping? mapping)
        {
            if (mapping != null)
            {
                CurrentFieldMapping = new FieldMapping
                {
                    Id = mapping.Id,
                    ExcelColumnName = mapping.ExcelColumnName,
                    DatabaseFieldName = mapping.DatabaseFieldName,
                    IsRequired = mapping.IsRequired,
                    DataType = mapping.DataType,
                    ForeignKeyMappingId = mapping.ForeignKeyMappingId,
                    ForeignKeyMapping = mapping.ForeignKeyMapping
                };
                IsEditingFieldMapping = true;
            }
        }

        private bool CanDeleteFieldMapping(FieldMapping? mapping)
        {
            return mapping != null && IsEditing && !IsEditingFieldMapping;
        }

        private void DeleteFieldMapping(FieldMapping? mapping)
        {
            if (mapping != null)
            {
                FieldMappings.Remove(mapping);
            }
        }

        private bool CanSaveFieldMapping()
        {
            return IsEditingFieldMapping;
        }

        private void SaveFieldMapping()
        {
            var validationResults = ValidateFieldMapping(CurrentFieldMapping);
            if (validationResults.Any())
            {
                ValidationErrors = string.Join(Environment.NewLine, validationResults);
                return;
            }

            var existingMapping = FieldMappings.FirstOrDefault(m => m.Id == CurrentFieldMapping.Id);
            if (existingMapping != null)
            {
                var index = FieldMappings.IndexOf(existingMapping);
                FieldMappings[index] = CurrentFieldMapping;
            }
            else
            {
                FieldMappings.Add(CurrentFieldMapping);
            }

            IsEditingFieldMapping = false;
            CurrentFieldMapping = new FieldMapping();
            ValidationErrors = string.Empty;
        }

        private void CancelFieldMappingEdit()
        {
            IsEditingFieldMapping = false;
            CurrentFieldMapping = new FieldMapping();
            ValidationErrors = string.Empty;
        }

        private async Task SelectExcelFileAsync()
        {
            // This would typically open a file dialog
            // For now, we'll simulate file selection
            // In a real implementation, you would use Microsoft.Win32.OpenFileDialog
            ValidationErrors = "Excel file selection would be implemented with a file dialog.";
        }

        private bool CanLoadExcelColumns()
        {
            return !string.IsNullOrWhiteSpace(SelectedExcelFilePath);
        }

        private async Task LoadExcelColumnsAsync()
        {
            try
            {
                var columns = await _excelReaderService.GetColumnNamesAsync(SelectedExcelFilePath);
                AvailableExcelColumns.Clear();
                foreach (var column in columns)
                {
                    AvailableExcelColumns.Add(column);
                }
            }
            catch (Exception ex)
            {
                ValidationErrors = $"Error loading Excel columns: {ex.Message}";
            }
        }

        private bool CanConfigureForeignKey()
        {
            return IsEditingFieldMapping && !IsConfiguringForeignKey;
        }

        private void ConfigureForeignKey()
        {
            if (CurrentFieldMapping.ForeignKeyMapping != null)
            {
                // Edit existing foreign key mapping
                CurrentForeignKeyMapping = new ForeignKeyMapping
                {
                    Id = CurrentFieldMapping.ForeignKeyMapping.Id,
                    ReferencedTable = CurrentFieldMapping.ForeignKeyMapping.ReferencedTable,
                    ReferencedLookupField = CurrentFieldMapping.ForeignKeyMapping.ReferencedLookupField,
                    ReferencedKeyField = CurrentFieldMapping.ForeignKeyMapping.ReferencedKeyField
                };
            }
            else
            {
                // Create new foreign key mapping
                CurrentForeignKeyMapping = new ForeignKeyMapping();
            }
            IsConfiguringForeignKey = true;
        }

        private bool CanSaveForeignKeyMapping()
        {
            return IsConfiguringForeignKey;
        }

        private void SaveForeignKeyMapping()
        {
            var validationResults = ValidateForeignKeyMapping(CurrentForeignKeyMapping);
            if (validationResults.Any())
            {
                ValidationErrors = string.Join(Environment.NewLine, validationResults);
                return;
            }

            CurrentFieldMapping.ForeignKeyMapping = CurrentForeignKeyMapping;
            IsConfiguringForeignKey = false;
            ValidationErrors = string.Empty;
        }

        private bool CanRemoveForeignKeyMapping()
        {
            return IsEditingFieldMapping && CurrentFieldMapping.ForeignKeyMapping != null && !IsConfiguringForeignKey;
        }

        private void RemoveForeignKeyMapping()
        {
            CurrentFieldMapping.ForeignKeyMapping = null;
            CurrentFieldMapping.ForeignKeyMappingId = null;
        }

        private void CancelForeignKeyConfiguration()
        {
            IsConfiguringForeignKey = false;
            CurrentForeignKeyMapping = new ForeignKeyMapping();
            ValidationErrors = string.Empty;
        }

        #endregion

        #region Helper Methods

        private void LoadFieldMappings()
        {
            FieldMappings.Clear();
            if (SelectedImportConfiguration?.FieldMappings != null)
            {
                foreach (var mapping in SelectedImportConfiguration.FieldMappings)
                {
                    FieldMappings.Add(mapping);
                }
            }
        }

        private List<string> ValidateImportConfiguration(ImportConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.Name))
                errors.Add("Configuration name is required.");

            if (config.DatabaseConfigurationId <= 0)
                errors.Add("Database configuration is required.");

            if (string.IsNullOrWhiteSpace(config.TableName))
                errors.Add("Table name is required.");

            if (!FieldMappings.Any())
                errors.Add("At least one field mapping is required.");

            // Validate that all required fields have corresponding Excel column mappings
            var requiredFieldsWithoutMapping = FieldMappings
                .Where(m => m.IsRequired && string.IsNullOrWhiteSpace(m.ExcelColumnName))
                .ToList();

            if (requiredFieldsWithoutMapping.Any())
            {
                errors.Add($"All required database fields must have Excel column mappings. Missing mappings for: {string.Join(", ", requiredFieldsWithoutMapping.Select(m => m.DatabaseFieldName))}");
            }

            // Check for duplicate names (excluding current configuration)
            var existingConfig = ImportConfigurations.FirstOrDefault(c => 
                c.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase) && c.Id != config.Id);
            if (existingConfig != null)
                errors.Add("A configuration with this name already exists.");

            return errors;
        }

        private List<string> ValidateFieldMapping(FieldMapping mapping)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(mapping.ExcelColumnName))
                errors.Add("Excel column name is required.");

            if (string.IsNullOrWhiteSpace(mapping.DatabaseFieldName))
                errors.Add("Database field name is required.");

            if (string.IsNullOrWhiteSpace(mapping.DataType))
                errors.Add("Data type is required.");

            // Check for duplicate Excel column mappings
            var existingMapping = FieldMappings.FirstOrDefault(m => 
                m.ExcelColumnName.Equals(mapping.ExcelColumnName, StringComparison.OrdinalIgnoreCase) && m.Id != mapping.Id);
            if (existingMapping != null)
                errors.Add("This Excel column is already mapped.");

            // Check for duplicate database field mappings
            existingMapping = FieldMappings.FirstOrDefault(m => 
                m.DatabaseFieldName.Equals(mapping.DatabaseFieldName, StringComparison.OrdinalIgnoreCase) && m.Id != mapping.Id);
            if (existingMapping != null)
                errors.Add("This database field is already mapped.");

            return errors;
        }

        private List<string> ValidateForeignKeyMapping(ForeignKeyMapping mapping)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(mapping.ReferencedTable))
                errors.Add("Referenced table is required for foreign key mapping.");

            if (string.IsNullOrWhiteSpace(mapping.ReferencedLookupField))
                errors.Add("Referenced lookup field is required for foreign key mapping.");

            if (string.IsNullOrWhiteSpace(mapping.ReferencedKeyField))
                errors.Add("Referenced key field is required for foreign key mapping.");

            return errors;
        }

        #endregion
    }
}