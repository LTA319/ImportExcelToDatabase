using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.ViewModels;
using ExcelDatabaseImportTool.Interfaces.Services;
using System.Collections.ObjectModel;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 4: Import configuration validation**
    /// **Validates: Requirements 2.2, 2.5**
    /// Property-based tests for import configuration validation
    /// </summary>
    [TestFixture]
    public class ImportConfigurationValidationTests
    {
        private MockConfigurationRepository _mockRepository = null!;
        private MockExcelReaderService _mockExcelService = null!;
        private ImportConfigurationViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new MockConfigurationRepository();
            _mockExcelService = new MockExcelReaderService();
            _viewModel = new ImportConfigurationViewModel(_mockRepository, _mockExcelService);
        }

        /// <summary>
        /// Property test: For any import configuration with all required fields having Excel column mappings,
        /// saving should succeed and the configuration should be persisted
        /// </summary>
        [Test]
        public void ValidImportConfigurationSavesSuccessfully()
        {
            // Arrange: Create a valid import configuration with all required fields mapped
            var dbConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "TestDB",
                Type = DatabaseType.MySQL,
                Server = "localhost",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted",
                Port = 3306,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { dbConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration>());

            // Load configurations
            var loadCommand = (Commands.AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            // Start adding a new import configuration
            var addCommand = (Commands.RelayCommand)_viewModel.AddImportConfigurationCommand;
            addCommand.Execute(null);

            // Set up the import configuration with valid data
            _viewModel.CurrentImportConfiguration.Name = "ValidImportConfig";
            _viewModel.CurrentImportConfiguration.DatabaseConfigurationId = dbConfig.Id;
            _viewModel.CurrentImportConfiguration.TableName = "Users";

            // Add field mappings where all required fields have Excel column mappings
            var requiredMapping1 = new FieldMapping
            {
                ExcelColumnName = "FirstName",
                DatabaseFieldName = "first_name",
                IsRequired = true,
                DataType = "string"
            };

            var requiredMapping2 = new FieldMapping
            {
                ExcelColumnName = "Email",
                DatabaseFieldName = "email",
                IsRequired = true,
                DataType = "string"
            };

            var optionalMapping = new FieldMapping
            {
                ExcelColumnName = "Phone",
                DatabaseFieldName = "phone",
                IsRequired = false,
                DataType = "string"
            };

            _viewModel.FieldMappings.Add(requiredMapping1);
            _viewModel.FieldMappings.Add(requiredMapping2);
            _viewModel.FieldMappings.Add(optionalMapping);

            // Act: Save the import configuration
            var saveCommand = (Commands.AsyncRelayCommand)_viewModel.SaveImportConfigurationCommand;
            saveCommand.ExecuteAsync().Wait();

            // Assert: Configuration should be saved successfully
            Assert.That(_viewModel.ValidationErrors, Is.Empty, 
                "No validation errors should occur for valid configuration");
            Assert.That(_viewModel.IsEditing, Is.False, 
                "Editing mode should be exited after successful save");
            Assert.That(_mockRepository.GetSavedImportConfigurations().Count, Is.EqualTo(1), 
                "Configuration should be persisted to repository");
            
            var savedConfig = _mockRepository.GetSavedImportConfigurations().First();
            Assert.That(savedConfig.Name, Is.EqualTo("ValidImportConfig"), 
                "Saved configuration should have correct name");
            Assert.That(savedConfig.FieldMappings.Count, Is.EqualTo(3), 
                "All field mappings should be saved");
        }

        /// <summary>
        /// Property test: For any import configuration with required fields missing Excel column mappings,
        /// saving should fail with appropriate validation error
        /// </summary>
        [Test]
        public void InvalidImportConfigurationFailsValidation()
        {
            // Arrange: Create an import configuration with required fields missing Excel column mappings
            var dbConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "TestDB",
                Type = DatabaseType.MySQL,
                Server = "localhost",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted",
                Port = 3306,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { dbConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration>());

            // Load configurations
            var loadCommand = (Commands.AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            // Start adding a new import configuration
            var addCommand = (Commands.RelayCommand)_viewModel.AddImportConfigurationCommand;
            addCommand.Execute(null);

            // Set up the import configuration with valid data
            _viewModel.CurrentImportConfiguration.Name = "InvalidImportConfig";
            _viewModel.CurrentImportConfiguration.DatabaseConfigurationId = dbConfig.Id;
            _viewModel.CurrentImportConfiguration.TableName = "Users";

            // Add field mappings where some required fields are missing Excel column mappings
            var validRequiredMapping = new FieldMapping
            {
                ExcelColumnName = "FirstName",
                DatabaseFieldName = "first_name",
                IsRequired = true,
                DataType = "string"
            };

            var invalidRequiredMapping = new FieldMapping
            {
                ExcelColumnName = "", // Missing Excel column name
                DatabaseFieldName = "email",
                IsRequired = true,
                DataType = "string"
            };

            _viewModel.FieldMappings.Add(validRequiredMapping);
            _viewModel.FieldMappings.Add(invalidRequiredMapping);

            // Act: Attempt to save the import configuration
            var saveCommand = (Commands.AsyncRelayCommand)_viewModel.SaveImportConfigurationCommand;
            saveCommand.ExecuteAsync().Wait();

            // Assert: Configuration should not be saved and validation error should be shown
            Assert.That(_viewModel.ValidationErrors, Is.Not.Empty, 
                "Validation errors should be present for invalid configuration");
            Assert.That(_viewModel.ValidationErrors.Contains("required"), Is.True, 
                "Error message should mention required fields");
            Assert.That(_viewModel.IsEditing, Is.True, 
                "Editing mode should remain active after failed save");
            Assert.That(_mockRepository.GetSavedImportConfigurations().Count, Is.EqualTo(0), 
                "Configuration should not be persisted to repository");
        }

        /// <summary>
        /// Property test: For any import configuration without field mappings,
        /// saving should fail with appropriate validation error
        /// </summary>
        [Test]
        public void ImportConfigurationWithoutFieldMappingsFailsValidation()
        {
            // Arrange: Create an import configuration without any field mappings
            var dbConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "TestDB",
                Type = DatabaseType.MySQL,
                Server = "localhost",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted",
                Port = 3306,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { dbConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration>());

            // Load configurations
            var loadCommand = (Commands.AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            // Start adding a new import configuration
            var addCommand = (Commands.RelayCommand)_viewModel.AddImportConfigurationCommand;
            addCommand.Execute(null);

            // Set up the import configuration with valid data but no field mappings
            _viewModel.CurrentImportConfiguration.Name = "NoMappingsConfig";
            _viewModel.CurrentImportConfiguration.DatabaseConfigurationId = dbConfig.Id;
            _viewModel.CurrentImportConfiguration.TableName = "Users";

            // Don't add any field mappings

            // Act: Attempt to save the import configuration
            var saveCommand = (Commands.AsyncRelayCommand)_viewModel.SaveImportConfigurationCommand;
            saveCommand.ExecuteAsync().Wait();

            // Assert: Configuration should not be saved and validation error should be shown
            Assert.That(_viewModel.ValidationErrors, Is.Not.Empty, 
                "Validation errors should be present for configuration without field mappings");
            Assert.That(_viewModel.ValidationErrors.Contains("field mapping"), Is.True, 
                "Error message should mention field mappings");
            Assert.That(_viewModel.IsEditing, Is.True, 
                "Editing mode should remain active after failed save");
            Assert.That(_mockRepository.GetSavedImportConfigurations().Count, Is.EqualTo(0), 
                "Configuration should not be persisted to repository");
        }

        /// <summary>
        /// Property test: For any import configuration with missing required metadata,
        /// saving should fail with appropriate validation error
        /// </summary>
        [Test]
        public void ImportConfigurationWithMissingMetadataFailsValidation()
        {
            // Arrange: Create an import configuration with missing required metadata
            var dbConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "TestDB",
                Type = DatabaseType.MySQL,
                Server = "localhost",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted",
                Port = 3306,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { dbConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration>());

            // Load configurations
            var loadCommand = (Commands.AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            // Start adding a new import configuration
            var addCommand = (Commands.RelayCommand)_viewModel.AddImportConfigurationCommand;
            addCommand.Execute(null);

            // Set up the import configuration with missing name
            _viewModel.CurrentImportConfiguration.Name = ""; // Missing name
            _viewModel.CurrentImportConfiguration.DatabaseConfigurationId = dbConfig.Id;
            _viewModel.CurrentImportConfiguration.TableName = "Users";

            // Add valid field mappings
            var validMapping = new FieldMapping
            {
                ExcelColumnName = "FirstName",
                DatabaseFieldName = "first_name",
                IsRequired = true,
                DataType = "string"
            };
            _viewModel.FieldMappings.Add(validMapping);

            // Act: Attempt to save the import configuration
            var saveCommand = (Commands.AsyncRelayCommand)_viewModel.SaveImportConfigurationCommand;
            saveCommand.ExecuteAsync().Wait();

            // Assert: Configuration should not be saved and validation error should be shown
            Assert.That(_viewModel.ValidationErrors, Is.Not.Empty, 
                "Validation errors should be present for configuration with missing metadata");
            Assert.That(_viewModel.ValidationErrors.Contains("name"), Is.True, 
                "Error message should mention missing name");
            Assert.That(_viewModel.IsEditing, Is.True, 
                "Editing mode should remain active after failed save");
            Assert.That(_mockRepository.GetSavedImportConfigurations().Count, Is.EqualTo(0), 
                "Configuration should not be persisted to repository");
        }

        #region Mock Classes

        private class MockConfigurationRepository : IConfigurationRepository
        {
            private List<DatabaseConfiguration> _databaseConfigurations = new();
            private List<ImportConfiguration> _importConfigurations = new();

            public void SetupDatabaseConfigurations(List<DatabaseConfiguration> configs)
            {
                _databaseConfigurations = new List<DatabaseConfiguration>(configs);
            }

            public void SetupImportConfigurations(List<ImportConfiguration> configs)
            {
                _importConfigurations = new List<ImportConfiguration>(configs);
            }

            public List<ImportConfiguration> GetSavedImportConfigurations()
            {
                return new List<ImportConfiguration>(_importConfigurations);
            }

            public Task<List<DatabaseConfiguration>> GetDatabaseConfigurationsAsync()
            {
                return Task.FromResult(new List<DatabaseConfiguration>(_databaseConfigurations));
            }

            public Task<DatabaseConfiguration?> GetDatabaseConfigurationByIdAsync(int id)
            {
                return Task.FromResult(_databaseConfigurations.FirstOrDefault(c => c.Id == id));
            }

            public Task<List<ImportConfiguration>> GetImportConfigurationsAsync()
            {
                return Task.FromResult(new List<ImportConfiguration>(_importConfigurations));
            }

            public Task<ImportConfiguration?> GetImportConfigurationByIdAsync(int id)
            {
                return Task.FromResult(_importConfigurations.FirstOrDefault(c => c.Id == id));
            }

            public Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config)
            {
                var existing = _databaseConfigurations.FirstOrDefault(c => c.Id == config.Id);
                if (existing != null)
                {
                    var index = _databaseConfigurations.IndexOf(existing);
                    _databaseConfigurations[index] = config;
                }
                else
                {
                    config.Id = _databaseConfigurations.Count + 1;
                    _databaseConfigurations.Add(config);
                }
                return Task.CompletedTask;
            }

            public Task SaveImportConfigurationAsync(ImportConfiguration config)
            {
                var existing = _importConfigurations.FirstOrDefault(c => c.Id == config.Id);
                if (existing != null)
                {
                    var index = _importConfigurations.IndexOf(existing);
                    _importConfigurations[index] = config;
                }
                else
                {
                    config.Id = _importConfigurations.Count + 1;
                    _importConfigurations.Add(config);
                }
                return Task.CompletedTask;
            }

            public Task DeleteDatabaseConfigurationAsync(int id)
            {
                var config = _databaseConfigurations.FirstOrDefault(c => c.Id == id);
                if (config != null)
                {
                    _databaseConfigurations.Remove(config);
                }
                return Task.CompletedTask;
            }

            public Task DeleteImportConfigurationAsync(int id)
            {
                var config = _importConfigurations.FirstOrDefault(c => c.Id == id);
                if (config != null)
                {
                    _importConfigurations.Remove(config);
                }
                return Task.CompletedTask;
            }

            public Task<bool> IsDatabaseConfigurationReferencedAsync(int id)
            {
                return Task.FromResult(_importConfigurations.Any(ic => ic.DatabaseConfigurationId == id));
            }
        }

        private class MockExcelReaderService : IExcelReaderService
        {
            public Task<System.Data.DataTable> ReadExcelFileAsync(string filePath)
            {
                throw new NotImplementedException();
            }

            public Task<List<string>> GetColumnNamesAsync(string filePath)
            {
                return Task.FromResult(new List<string> { "FirstName", "LastName", "Email", "Phone" });
            }

            public Task<bool> ValidateFileAsync(string filePath)
            {
                return Task.FromResult(true);
            }
        }

        #endregion
    }
}
