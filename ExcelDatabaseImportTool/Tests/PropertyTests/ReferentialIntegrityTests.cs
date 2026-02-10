using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.ViewModels;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Commands;
using System.Collections.ObjectModel;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 3: Referential integrity enforcement**
    /// **Validates: Requirements 1.5**
    /// Property-based tests for referential integrity enforcement in database configurations
    /// </summary>
    [TestFixture]
    public class ReferentialIntegrityTests
    {
        private MockConfigurationRepository _mockRepository = null!;
        private MockDatabaseConnectionService _mockConnectionService = null!;
        private DatabaseConfigurationViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new MockConfigurationRepository();
            _mockConnectionService = new MockDatabaseConnectionService();
            _viewModel = new DatabaseConfigurationViewModel(_mockRepository, _mockConnectionService);
        }

        /// <summary>
        /// Property test: For any database configuration that is referenced by import configurations,
        /// attempting to delete it should be prevented and the configuration should remain in storage
        /// </summary>
        [Test]
        public void ReferentialIntegrityEnforcement()
        {
            // Arrange: Create a referenced database configuration
            var referencedConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "ReferencedConfig",
                Type = DatabaseType.MySQL,
                Server = "server.test.com",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted_pass",
                Port = 3306,
                ConnectionString = "Server=server.test.com;Database=testdb;",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            var importConfig = new ImportConfiguration
            {
                Id = 1,
                Name = "ImportConfig",
                DatabaseConfigurationId = referencedConfig.Id,
                TableName = "test_table",
                HasHeaderRow = true,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                FieldMappings = new List<FieldMapping>()
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { referencedConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration> { importConfig });
            _mockRepository.SetupReferenceCheck(referencedConfig.Id, true);

            // Act: Load configurations and attempt to delete the referenced configuration
            var loadCommand = (AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            var initialCount = _viewModel.Configurations.Count;
            var deleteCommand = (AsyncRelayCommand<DatabaseConfiguration>)_viewModel.DeleteConfigurationCommand;
            deleteCommand.ExecuteAsync(referencedConfig).Wait();

            // Assert: The configuration should still exist and an error should be shown
            Assert.That(_viewModel.Configurations.Any(c => c.Id == referencedConfig.Id), Is.True, 
                "Referenced configuration should still exist after delete attempt");
            Assert.That(_viewModel.ValidationErrors, Is.Not.Empty, 
                "Validation error should be shown");
            Assert.That(_viewModel.ValidationErrors.Contains("referenced"), Is.True, 
                "Error message should mention that configuration is referenced");
            Assert.That(_viewModel.Configurations.Count, Is.EqualTo(initialCount), 
                "Configuration count should remain the same");
        }

        /// <summary>
        /// Property test: For any database configuration that is NOT referenced by import configurations,
        /// attempting to delete it should succeed and the configuration should be removed from storage
        /// </summary>
        [Test]
        public void UnreferencedConfigurationDeletion()
        {
            // Arrange: Create an unreferenced database configuration
            var unreferencedConfig = new DatabaseConfiguration
            {
                Id = 1,
                Name = "UnreferencedConfig",
                Type = DatabaseType.MySQL,
                Server = "server.test.com",
                Database = "testdb",
                Username = "user",
                EncryptedPassword = "encrypted_pass",
                Port = 3306,
                ConnectionString = "Server=server.test.com;Database=testdb;",
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _mockRepository.SetupDatabaseConfigurations(new List<DatabaseConfiguration> { unreferencedConfig });
            _mockRepository.SetupImportConfigurations(new List<ImportConfiguration>());
            _mockRepository.SetupReferenceCheck(unreferencedConfig.Id, false);

            // Act: Load configurations and attempt to delete the unreferenced configuration
            var loadCommand = (AsyncRelayCommand)_viewModel.LoadConfigurationsCommand;
            loadCommand.ExecuteAsync().Wait();

            var initialCount = _viewModel.Configurations.Count;
            var deleteCommand = (AsyncRelayCommand<DatabaseConfiguration>)_viewModel.DeleteConfigurationCommand;
            deleteCommand.ExecuteAsync(unreferencedConfig).Wait();

            // Assert: The configuration should be removed and no error should be shown
            Assert.That(_viewModel.Configurations.Any(c => c.Id == unreferencedConfig.Id), Is.False, 
                "Unreferenced configuration should be removed after delete");
            Assert.That(_viewModel.ValidationErrors, Is.Empty, 
                "No validation error should be shown");
            Assert.That(_viewModel.Configurations.Count, Is.EqualTo(initialCount - 1), 
                "Configuration count should decrease by one");
        }

        #region Mock Classes

        private class MockConfigurationRepository : IConfigurationRepository
        {
            private List<DatabaseConfiguration> _databaseConfigurations = new();
            private List<ImportConfiguration> _importConfigurations = new();
            private Dictionary<int, bool> _referenceChecks = new();

            public void SetupDatabaseConfigurations(List<DatabaseConfiguration> configs)
            {
                _databaseConfigurations = new List<DatabaseConfiguration>(configs);
            }

            public void SetupImportConfigurations(List<ImportConfiguration> configs)
            {
                _importConfigurations = new List<ImportConfiguration>(configs);
            }

            public void SetupReferenceCheck(int configId, bool isReferenced)
            {
                _referenceChecks[configId] = isReferenced;
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
                return Task.FromResult(_referenceChecks.GetValueOrDefault(id, false));
            }
        }

        private class MockDatabaseConnectionService : IDatabaseConnectionService
        {
            public Task<bool> TestConnectionAsync(DatabaseConfiguration config)
            {
                return Task.FromResult(true);
            }

            public Task<System.Data.IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
            {
                throw new NotImplementedException();
            }

            public string BuildConnectionString(DatabaseConfiguration config)
            {
                return $"Server={config.Server};Database={config.Database};";
            }
        }

        #endregion
    }
}