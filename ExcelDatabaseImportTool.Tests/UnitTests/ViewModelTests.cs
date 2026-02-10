using NUnit.Framework;
using Moq;
using ExcelDatabaseImportTool.ViewModels;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using System.Collections.ObjectModel;

namespace ExcelDatabaseImportTool.Tests.UnitTests
{
    /// <summary>
    /// Unit tests for ViewModel classes
    /// Tests commands, properties, data binding, and validation logic
    /// </summary>
    [TestFixture]
    public class ViewModelTests
    {
        #region DatabaseConfigurationViewModel Tests

        [Test]
        public async Task DatabaseConfigurationViewModel_LoadConfigurations_PopulatesCollection()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            
            var testConfigs = new List<DatabaseConfiguration>
            {
                new DatabaseConfiguration { Id = 1, Name = "Test DB 1" },
                new DatabaseConfiguration { Id = 2, Name = "Test DB 2" }
            };
            
            mockRepo.Setup(r => r.GetDatabaseConfigurationsAsync())
                .ReturnsAsync(testConfigs);
            
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            
            // Act
            await Task.Delay(100); // Wait for initialization
            
            // Assert
            Assert.That(viewModel.Configurations.Count, Is.EqualTo(2));
            Assert.That(viewModel.Configurations[0].Name, Is.EqualTo("Test DB 1"));
        }

        [Test]
        public void DatabaseConfigurationViewModel_AddConfiguration_SetsEditingMode()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            
            // Act
            viewModel.AddConfigurationCommand.Execute(null);
            
            // Assert
            Assert.That(viewModel.IsEditing, Is.True);
            Assert.That(viewModel.CurrentConfiguration, Is.Not.Null);
        }

        [Test]
        public async Task DatabaseConfigurationViewModel_SaveConfiguration_ValidatesRequiredFields()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            
            viewModel.AddConfigurationCommand.Execute(null);
            viewModel.CurrentConfiguration.Name = ""; // Invalid - empty name
            
            // Act
            await ((Commands.AsyncRelayCommand)viewModel.SaveConfigurationCommand).ExecuteAsync();
            
            // Assert
            Assert.That(viewModel.ValidationErrors, Is.Not.Empty);
            Assert.That(viewModel.ValidationErrors, Does.Contain("name"));
        }

        [Test]
        public async Task DatabaseConfigurationViewModel_TestConnection_CallsConnectionService()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            
            mockConnectionService.Setup(s => s.TestConnectionAsync(It.IsAny<DatabaseConfiguration>()))
                .ReturnsAsync(true);
            
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            viewModel.AddConfigurationCommand.Execute(null);
            viewModel.CurrentConfiguration.Server = "localhost";
            
            // Act
            await ((Commands.AsyncRelayCommand)viewModel.TestConnectionCommand).ExecuteAsync();
            
            // Assert
            mockConnectionService.Verify(s => s.TestConnectionAsync(It.IsAny<DatabaseConfiguration>()), Times.Once);
            Assert.That(viewModel.ConnectionTestResult, Does.Contain("successful"));
        }

        #endregion

        #region ImportConfigurationViewModel Tests

        [Test]
        public async Task ImportConfigurationViewModel_LoadConfigurations_PopulatesBothCollections()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockExcelService = new Mock<IExcelReaderService>();
            
            var testImportConfigs = new List<ImportConfiguration>
            {
                new ImportConfiguration { Id = 1, Name = "Import 1" }
            };
            
            var testDbConfigs = new List<DatabaseConfiguration>
            {
                new DatabaseConfiguration { Id = 1, Name = "DB 1" }
            };
            
            mockRepo.Setup(r => r.GetImportConfigurationsAsync())
                .ReturnsAsync(testImportConfigs);
            mockRepo.Setup(r => r.GetDatabaseConfigurationsAsync())
                .ReturnsAsync(testDbConfigs);
            
            var viewModel = new ImportConfigurationViewModel(mockRepo.Object, mockExcelService.Object);
            
            // Act
            await Task.Delay(100); // Wait for initialization
            
            // Assert
            Assert.That(viewModel.ImportConfigurations.Count, Is.EqualTo(1));
            Assert.That(viewModel.DatabaseConfigurations.Count, Is.EqualTo(1));
        }

        [Test]
        public void ImportConfigurationViewModel_AddFieldMapping_EntersEditMode()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockExcelService = new Mock<IExcelReaderService>();
            var viewModel = new ImportConfigurationViewModel(mockRepo.Object, mockExcelService.Object);
            
            viewModel.AddImportConfigurationCommand.Execute(null);
            
            // Act
            viewModel.AddFieldMappingCommand.Execute(null);
            
            // Assert
            Assert.That(viewModel.IsEditingFieldMapping, Is.True);
            Assert.That(viewModel.CurrentFieldMapping, Is.Not.Null);
        }

        [Test]
        public void ImportConfigurationViewModel_SaveFieldMapping_AddsToCollection()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockExcelService = new Mock<IExcelReaderService>();
            var viewModel = new ImportConfigurationViewModel(mockRepo.Object, mockExcelService.Object);
            
            viewModel.AddImportConfigurationCommand.Execute(null);
            viewModel.AddFieldMappingCommand.Execute(null);
            
            viewModel.CurrentFieldMapping.ExcelColumnName = "Column1";
            viewModel.CurrentFieldMapping.DatabaseFieldName = "Field1";
            viewModel.CurrentFieldMapping.DataType = "string";
            
            // Act
            viewModel.SaveFieldMappingCommand.Execute(null);
            
            // Assert
            Assert.That(viewModel.FieldMappings.Count, Is.EqualTo(1));
            Assert.That(viewModel.FieldMappings[0].ExcelColumnName, Is.EqualTo("Column1"));
            Assert.That(viewModel.IsEditingFieldMapping, Is.False);
        }

        #endregion

        #region ImportExecutionViewModel Tests

        [Test]
        public async Task ImportExecutionViewModel_LoadConfigurations_PopulatesCollection()
        {
            // Arrange
            var mockConfigRepo = new Mock<IConfigurationRepository>();
            var mockImportService = new Mock<IImportService>();
            var mockLogRepo = new Mock<IImportLogRepository>();
            
            var testConfigs = new List<ImportConfiguration>
            {
                new ImportConfiguration { Id = 1, Name = "Config 1" }
            };
            
            mockConfigRepo.Setup(r => r.GetImportConfigurationsAsync())
                .ReturnsAsync(testConfigs);
            
            var viewModel = new ImportExecutionViewModel(mockConfigRepo.Object, mockImportService.Object, mockLogRepo.Object);
            
            // Act
            await Task.Delay(100); // Wait for initialization
            
            // Assert
            Assert.That(viewModel.ImportConfigurations.Count, Is.EqualTo(1));
        }

        [Test]
        public void ImportExecutionViewModel_ExecuteImportCommand_RequiresConfigurationAndFile()
        {
            // Arrange
            var mockConfigRepo = new Mock<IConfigurationRepository>();
            var mockImportService = new Mock<IImportService>();
            var mockLogRepo = new Mock<IImportLogRepository>();
            
            var viewModel = new ImportExecutionViewModel(mockConfigRepo.Object, mockImportService.Object, mockLogRepo.Object);
            
            // Act & Assert - No configuration or file selected
            Assert.That(viewModel.ExecuteImportCommand.CanExecute(null), Is.False);
            
            // Set configuration but no file
            viewModel.SelectedImportConfiguration = new ImportConfiguration { Id = 1, Name = "Test" };
            Assert.That(viewModel.ExecuteImportCommand.CanExecute(null), Is.False);
        }

        [Test]
        public void ImportExecutionViewModel_ClearResults_ResetsResultProperties()
        {
            // Arrange
            var mockConfigRepo = new Mock<IConfigurationRepository>();
            var mockImportService = new Mock<IImportService>();
            var mockLogRepo = new Mock<IImportLogRepository>();
            
            var viewModel = new ImportExecutionViewModel(mockConfigRepo.Object, mockImportService.Object, mockLogRepo.Object);
            
            // Set some result data
            typeof(ImportExecutionViewModel).GetProperty("TotalRecords")!.SetValue(viewModel, 100);
            typeof(ImportExecutionViewModel).GetProperty("SuccessfulRecords")!.SetValue(viewModel, 90);
            typeof(ImportExecutionViewModel).GetProperty("FailedRecords")!.SetValue(viewModel, 10);
            typeof(ImportExecutionViewModel).GetProperty("HasResults")!.SetValue(viewModel, true);
            
            // Act
            viewModel.ClearResultsCommand.Execute(null);
            
            // Assert
            Assert.That(viewModel.TotalRecords, Is.EqualTo(0));
            Assert.That(viewModel.SuccessfulRecords, Is.EqualTo(0));
            Assert.That(viewModel.FailedRecords, Is.EqualTo(0));
            Assert.That(viewModel.HasResults, Is.False);
        }

        #endregion

        #region Property Change Notification Tests

        [Test]
        public void DatabaseConfigurationViewModel_PropertyChanged_RaisesEvent()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            
            var propertyChangedRaised = false;
            string? changedPropertyName = null;
            
            viewModel.PropertyChanged += (sender, args) =>
            {
                propertyChangedRaised = true;
                changedPropertyName = args.PropertyName;
            };
            
            // Act
            viewModel.AddConfigurationCommand.Execute(null);
            
            // Assert
            Assert.That(propertyChangedRaised, Is.True);
            // IsEditing property should have changed
        }

        #endregion

        #region Command CanExecute Tests

        [Test]
        public void DatabaseConfigurationViewModel_EditCommand_RequiresNonNullConfiguration()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockConnectionService = new Mock<IDatabaseConnectionService>();
            var viewModel = new DatabaseConfigurationViewModel(mockRepo.Object, mockConnectionService.Object);
            
            // Act & Assert
            Assert.That(viewModel.EditConfigurationCommand.CanExecute(null), Is.False);
            
            var config = new DatabaseConfiguration { Id = 1, Name = "Test" };
            Assert.That(viewModel.EditConfigurationCommand.CanExecute(config), Is.True);
        }

        [Test]
        public void ImportConfigurationViewModel_SaveFieldMappingCommand_RequiresEditMode()
        {
            // Arrange
            var mockRepo = new Mock<IConfigurationRepository>();
            var mockExcelService = new Mock<IExcelReaderService>();
            var viewModel = new ImportConfigurationViewModel(mockRepo.Object, mockExcelService.Object);
            
            // Act & Assert - Not in edit mode
            Assert.That(viewModel.SaveFieldMappingCommand.CanExecute(null), Is.False);
            
            // Enter edit mode
            viewModel.AddImportConfigurationCommand.Execute(null);
            viewModel.AddFieldMappingCommand.Execute(null);
            Assert.That(viewModel.SaveFieldMappingCommand.CanExecute(null), Is.True);
        }

        #endregion
    }
}
