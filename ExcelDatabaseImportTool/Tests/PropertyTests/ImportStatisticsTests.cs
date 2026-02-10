using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Repositories;
using ExcelDatabaseImportTool.Services.Import;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 9: Import statistics accuracy**
    /// **Validates: Requirements 3.5**
    /// </summary>
    public static class ImportStatisticsTests
    {
        public static void RunImportStatisticsTests()
        {
            var results = new List<string>();
            results.Add("Running import statistics tests...");

            try
            {
                // Test that import statistics accurately reflect processed records
                TestStatisticsAccuracy(results);
                
                results.Add("Import statistics tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during import statistics tests: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }
            
            // Write results to file
            File.WriteAllLines("import_statistics_test_results.txt", results);
        }

        private static void TestStatisticsAccuracy(List<string> results)
        {
            try
            {
                // Test with different combinations of successful and failed records
                var testCases = new[]
                {
                    new { Description = "All successful records", SuccessfulCount = 5, FailedCount = 0 },
                    new { Description = "All failed records", SuccessfulCount = 0, FailedCount = 3 },
                    new { Description = "Mixed success and failure", SuccessfulCount = 3, FailedCount = 2 },
                    new { Description = "Single successful record", SuccessfulCount = 1, FailedCount = 0 },
                    new { Description = "Single failed record", SuccessfulCount = 0, FailedCount = 1 }
                };

                foreach (var testCase in testCases)
                {
                    results.Add($"Testing: {testCase.Description}");
                    
                    // Create test data based on the test case
                    var testData = CreateTestDataForStatistics(testCase.SuccessfulCount, testCase.FailedCount);
                    var tempExcelFile = CreateTempExcelFile(testData);
                    
                    // Create test configuration
                    var config = CreateTestImportConfiguration();
                    
                    // Create in-memory database for testing
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                        .Options;

                    using var context = new ApplicationDbContext(options);
                    context.Database.EnsureCreated();
                    
                    // Set up repositories and services
                    var configRepo = new ConfigurationRepository(context);
                    var logRepo = new ImportLogRepository(context);
                    
                    // Save database configuration
                    var dbConfig = CreateTestDatabaseConfiguration();
                    configRepo.SaveDatabaseConfigurationAsync(dbConfig).Wait();
                    config.DatabaseConfigurationId = dbConfig.Id;
                    configRepo.SaveImportConfigurationAsync(config).Wait();

                    // Create mock services that will produce predictable results
                    var mockExcelService = new MockExcelReaderServiceForStatistics(testData);
                    var mockValidationService = new MockValidationServiceForStatistics(testCase.SuccessfulCount, testCase.FailedCount);
                    var mockForeignKeyService = new MockForeignKeyResolverServiceForStatistics();
                    var mockDbConnectionService = new MockDatabaseConnectionServiceForStatistics();

                    var importService = new ImportService(
                        mockExcelService,
                        mockValidationService,
                        mockForeignKeyService,
                        mockDbConnectionService,
                        logRepo,
                        configRepo);

                    // Act - Perform import
                    var result = importService.ImportDataAsync(config, tempExcelFile).Result;

                    // Assert - Verify statistics accuracy
                    var expectedTotal = testCase.SuccessfulCount + testCase.FailedCount;
                    
                    if (result.TotalRecords != expectedTotal)
                    {
                        results.Add($"  FAIL: Total records incorrect. Expected: {expectedTotal}, Actual: {result.TotalRecords}");
                        continue;
                    }

                    if (result.SuccessfulRecords != testCase.SuccessfulCount)
                    {
                        results.Add($"  FAIL: Successful records incorrect. Expected: {testCase.SuccessfulCount}, Actual: {result.SuccessfulRecords}");
                        continue;
                    }

                    if (result.FailedRecords != testCase.FailedCount)
                    {
                        results.Add($"  FAIL: Failed records incorrect. Expected: {testCase.FailedCount}, Actual: {result.FailedRecords}");
                        continue;
                    }

                    // Verify that total = successful + failed
                    var calculatedTotal = result.SuccessfulRecords + result.FailedRecords;
                    if (calculatedTotal != result.TotalRecords)
                    {
                        results.Add($"  FAIL: Statistics don't add up. Total: {result.TotalRecords}, Successful + Failed: {calculatedTotal}");
                        continue;
                    }

                    // Verify error collection matches failed count
                    if (testCase.FailedCount > 0 && result.Errors.Count == 0)
                    {
                        results.Add($"  FAIL: Expected error details for {testCase.FailedCount} failed records, but got none");
                        continue;
                    }

                    if (testCase.FailedCount == 0 && result.Errors.Count > 0)
                    {
                        results.Add($"  FAIL: Expected no errors for successful import, but got {result.Errors.Count} errors");
                        continue;
                    }

                    results.Add($"  PASS: Statistics accurate - Total: {result.TotalRecords}, Successful: {result.SuccessfulRecords}, Failed: {result.FailedRecords}");
                    
                    // Cleanup
                    File.Delete(tempExcelFile);
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during statistics accuracy test: {ex.Message}");
            }
        }

        // Helper methods for creating test data and configurations
        private static DataTable CreateTestDataForStatistics(int successfulCount, int failedCount)
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Age", typeof(int));

            // Add successful records
            for (int i = 0; i < successfulCount; i++)
            {
                table.Rows.Add($"User{i + 1}", $"user{i + 1}@example.com", 25 + i);
            }

            // Add failed records (will be marked as invalid by validation service)
            for (int i = 0; i < failedCount; i++)
            {
                table.Rows.Add("", "invalid-email", -1); // Invalid data
            }

            return table;
        }

        private static string CreateTempExcelFile(DataTable data)
        {
            var tempFile = Path.GetTempFileName() + ".xlsx";
            File.WriteAllText(tempFile, "Mock Excel File");
            return tempFile;
        }

        private static ImportConfiguration CreateTestImportConfiguration()
        {
            return new ImportConfiguration
            {
                Name = "Test Import Config",
                TableName = "TestTable",
                HasHeaderRow = true,
                FieldMappings = new List<FieldMapping>
                {
                    new FieldMapping
                    {
                        ExcelColumnName = "Name",
                        DatabaseFieldName = "Name",
                        IsRequired = true,
                        DataType = "string"
                    },
                    new FieldMapping
                    {
                        ExcelColumnName = "Email",
                        DatabaseFieldName = "Email",
                        IsRequired = true,
                        DataType = "string"
                    },
                    new FieldMapping
                    {
                        ExcelColumnName = "Age",
                        DatabaseFieldName = "Age",
                        IsRequired = false,
                        DataType = "int"
                    }
                },
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
        }

        private static DatabaseConfiguration CreateTestDatabaseConfiguration()
        {
            return new DatabaseConfiguration
            {
                Name = "Test Database",
                Type = DatabaseType.SqlServer,
                Server = "localhost",
                Database = "TestDB",
                Username = "testuser",
                EncryptedPassword = "encrypted_password",
                Port = 1433,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
        }
    }

    // Mock service implementations for statistics testing
    public class MockExcelReaderServiceForStatistics : ExcelDatabaseImportTool.Interfaces.Services.IExcelReaderService
    {
        private readonly DataTable _testData;

        public MockExcelReaderServiceForStatistics(DataTable testData)
        {
            _testData = testData;
        }

        public Task<DataTable> ReadExcelFileAsync(string filePath)
        {
            return Task.FromResult(_testData);
        }

        public Task<List<string>> GetColumnNamesAsync(string filePath)
        {
            var columnNames = _testData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            return Task.FromResult(columnNames);
        }

        public Task<bool> ValidateFileAsync(string filePath)
        {
            return Task.FromResult(true);
        }
    }

    public class MockValidationServiceForStatistics : ExcelDatabaseImportTool.Interfaces.Services.IValidationService
    {
        private readonly int _expectedSuccessful;
        private readonly int _expectedFailed;
        private int _processedCount = 0;

        public MockValidationServiceForStatistics(int expectedSuccessful, int expectedFailed)
        {
            _expectedSuccessful = expectedSuccessful;
            _expectedFailed = expectedFailed;
        }

        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateDataRowAsync(DataRow row, List<FieldMapping> fieldMappings)
        {
            var result = new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult();
            
            // Return success for the first N records, then failure for the rest
            if (_processedCount < _expectedSuccessful)
            {
                result.IsValid = true;
            }
            else
            {
                result.IsValid = false;
                result.Errors.Add($"Simulated validation error for record {_processedCount + 1}");
            }

            _processedCount++;
            return Task.FromResult(result);
        }

        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateImportConfigurationAsync(ImportConfiguration config)
        {
            return Task.FromResult(new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult { IsValid = true });
        }
    }

    public class MockForeignKeyResolverServiceForStatistics : ExcelDatabaseImportTool.Interfaces.Services.IForeignKeyResolverService
    {
        public Task<object?> ResolveForeignKeyAsync(string lookupValue, ForeignKeyMapping mapping, DatabaseConfiguration dbConfig)
        {
            return Task.FromResult<object?>(1);
        }

        public Task<Dictionary<string, object>> ResolveForeignKeysAsync(Dictionary<string, string> lookupValues, List<ForeignKeyMapping> mappings, DatabaseConfiguration dbConfig)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in lookupValues)
            {
                result[kvp.Key] = 1;
            }
            return Task.FromResult(result);
        }
    }

    public class MockDatabaseConnectionServiceForStatistics : ExcelDatabaseImportTool.Interfaces.Services.IDatabaseConnectionService
    {
        public Task<bool> TestConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult(true);
        }

        public Task<System.Data.IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult<System.Data.IDbConnection>(new MockDbConnectionForStatistics());
        }

        public string BuildConnectionString(DatabaseConfiguration config)
        {
            return "mock connection string";
        }
    }

    // Mock database connection for statistics testing
    public class MockDbConnectionForStatistics : System.Data.IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "MockDB";
        public System.Data.ConnectionState State => System.Data.ConnectionState.Open;

        public System.Data.IDbTransaction BeginTransaction() => new MockDbTransactionForStatistics();
        public System.Data.IDbTransaction BeginTransaction(System.Data.IsolationLevel il) => new MockDbTransactionForStatistics();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public System.Data.IDbCommand CreateCommand() => new MockDbCommandForStatistics();
        public void Dispose() { }
        public void Open() { }
        public Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class MockDbTransactionForStatistics : System.Data.IDbTransaction
    {
        public System.Data.IDbConnection Connection => new MockDbConnectionForStatistics();
        public System.Data.IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public void Commit() { }
        public void Dispose() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class MockDbCommandForStatistics : System.Data.IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; } = 30;
        public System.Data.CommandType CommandType { get; set; } = System.Data.CommandType.Text;
        public System.Data.IDbConnection? Connection { get; set; }
        public System.Data.IDataParameterCollection Parameters { get; } = new MockParameterCollectionForStatistics();
        public System.Data.IDbTransaction? Transaction { get; set; }
        public System.Data.UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public System.Data.IDbDataParameter CreateParameter() => new MockDbParameterForStatistics();
        public void Dispose() { }
        public int ExecuteNonQuery() => 1;
        public System.Data.IDataReader ExecuteReader() => throw new NotImplementedException();
        public System.Data.IDataReader ExecuteReader(System.Data.CommandBehavior behavior) => throw new NotImplementedException();
        public object ExecuteScalar() => 1;
        public void Prepare() { }
        public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    public class MockDbParameterForStatistics : System.Data.IDbDataParameter
    {
        public System.Data.DbType DbType { get; set; }
        public System.Data.ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = "";
        public string SourceColumn { get; set; } = "";
        public System.Data.DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }

    public class MockParameterCollectionForStatistics : System.Data.IDataParameterCollection
    {
        private readonly List<System.Data.IDataParameter> _parameters = new();

        public object? this[string parameterName] { get => _parameters.FirstOrDefault(p => p.ParameterName == parameterName); set => throw new NotImplementedException(); }
        public object? this[int index] { get => _parameters[index]; set => _parameters[index] = (System.Data.IDataParameter)value!; }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public int Count => _parameters.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public int Add(object? value)
        {
            if (value != null)
            {
                _parameters.Add((System.Data.IDataParameter)value);
                return _parameters.Count - 1;
            }
            return -1;
        }

        public void Clear() => _parameters.Clear();
        public bool Contains(object? value) => value != null && _parameters.Contains((System.Data.IDataParameter)value);
        public bool Contains(string parameterName) => _parameters.Any(p => p.ParameterName == parameterName);
        public void CopyTo(Array array, int index) => throw new NotImplementedException();
        public System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public int IndexOf(object? value) => value != null ? _parameters.IndexOf((System.Data.IDataParameter)value) : -1;
        public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public void Insert(int index, object? value) { if (value != null) _parameters.Insert(index, (System.Data.IDataParameter)value); }
        public void Remove(object? value) { if (value != null) _parameters.Remove((System.Data.IDataParameter)value); }
        public void RemoveAt(int index) => _parameters.RemoveAt(index);
        public void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0) RemoveAt(index);
        }
    }
}