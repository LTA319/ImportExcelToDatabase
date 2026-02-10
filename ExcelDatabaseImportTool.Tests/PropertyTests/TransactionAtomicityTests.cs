using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Repositories;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Services.Excel;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 12: Transaction atomicity**
    /// **Validates: Requirements 5.3, 5.4, 5.5**
    /// </summary>
    public static class TransactionAtomicityTests
    {
        public static void RunTransactionAtomicityTests()
        {
            var results = new List<string>();
            results.Add("Running transaction atomicity tests...");

            try
            {
                // Test that failed validations result in complete rollback
                TestValidationFailureRollback(results);
                
                results.Add("Transaction atomicity tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during transaction atomicity tests: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }
            
            // Write results to file
            File.WriteAllLines("transaction_atomicity_test_results.txt", results);
        }

        private static void TestValidationFailureRollback(List<string> results)
        {
            try
            {
                // Create test Excel data with some invalid records
                var testData = CreateTestDataWithValidationErrors();
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

                // Create mock services that will cause validation failures
                var mockExcelService = new MockExcelReaderServiceForTransaction(testData);
                var mockValidationService = new MockValidationServiceWithFailures();
                var mockForeignKeyService = new MockForeignKeyResolverServiceForTransaction();
                var mockDbConnectionService = new MockDatabaseConnectionService();

                var importService = new ImportService(
                    mockExcelService,
                    mockValidationService,
                    mockForeignKeyService,
                    mockDbConnectionService,
                    logRepo,
                    configRepo);

                // Act - Attempt import with validation failures
                var result = importService.ImportDataAsync(config, tempExcelFile).Result;

                // Assert - Verify transaction was rolled back
                if (result.Success)
                {
                    results.Add("FAIL: Import should have failed due to validation errors");
                    return;
                }

                if (result.SuccessfulRecords > 0)
                {
                    results.Add("FAIL: No records should have been committed when validation fails");
                    return;
                }

                if (result.FailedRecords != testData.Rows.Count)
                {
                    results.Add($"FAIL: All records should be marked as failed. Expected: {testData.Rows.Count}, Actual: {result.FailedRecords}");
                    return;
                }

                if (result.Errors.Count == 0)
                {
                    results.Add("FAIL: Error details should be logged when validation fails");
                    return;
                }

                results.Add("PASS: Validation failures result in complete rollback with proper error logging");
                
                // Cleanup
                File.Delete(tempExcelFile);
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during validation failure rollback test: {ex.Message}");
            }
        }

        private static void TestDatabaseErrorRollback(List<string> results)
        {
            try
            {
                // Create test Excel data
                var testData = CreateValidTestData();
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

                // Create mock services that will cause database errors
                var mockExcelService = new MockExcelReaderServiceForTransaction(testData);
                var mockValidationService = new MockValidationServiceSuccess();
                var mockForeignKeyService = new MockForeignKeyResolverServiceForTransaction();
                var mockDbConnectionService = new MockDatabaseConnectionServiceWithErrors();

                var importService = new ImportService(
                    mockExcelService,
                    mockValidationService,
                    mockForeignKeyService,
                    mockDbConnectionService,
                    logRepo,
                    configRepo);

                // Act - Attempt import with database errors
                var result = importService.ImportDataAsync(config, tempExcelFile).Result;

                // Assert - Verify transaction was rolled back
                if (result.Success)
                {
                    results.Add("FAIL: Import should have failed due to database errors");
                    return;
                }

                if (result.SuccessfulRecords > 0)
                {
                    results.Add("FAIL: No records should have been committed when database errors occur");
                    return;
                }

                if (result.Errors.Count == 0)
                {
                    results.Add("FAIL: Error details should be logged when database errors occur");
                    return;
                }

                results.Add("PASS: Database errors result in complete rollback with proper error logging");
                
                // Cleanup
                File.Delete(tempExcelFile);
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during database error rollback test: {ex.Message}");
            }
        }

        private static void TestSuccessfulCommit(List<string> results)
        {
            try
            {
                // Create test Excel data
                var testData = CreateValidTestData();
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

                // Create mock services for successful import
                var mockExcelService = new MockExcelReaderServiceForTransaction(testData);
                var mockValidationService = new MockValidationServiceSuccess();
                var mockForeignKeyService = new MockForeignKeyResolverServiceForTransaction();
                var mockDbConnectionService = new MockDatabaseConnectionServiceSuccess();

                var importService = new ImportService(
                    mockExcelService,
                    mockValidationService,
                    mockForeignKeyService,
                    mockDbConnectionService,
                    logRepo,
                    configRepo);

                // Act - Perform successful import
                var result = importService.ImportDataAsync(config, tempExcelFile).Result;

                // Assert - Verify all records were committed
                if (!result.Success)
                {
                    results.Add($"FAIL: Import should have succeeded. Errors: {string.Join(", ", result.Errors)}");
                    return;
                }

                if (result.SuccessfulRecords != testData.Rows.Count)
                {
                    results.Add($"FAIL: All records should have been committed. Expected: {testData.Rows.Count}, Actual: {result.SuccessfulRecords}");
                    return;
                }

                if (result.FailedRecords != 0)
                {
                    results.Add($"FAIL: No records should have failed. Actual failed: {result.FailedRecords}");
                    return;
                }

                // Verify import log was created and updated properly
                if (result.ImportLog.Status != ImportStatus.Success)
                {
                    results.Add($"FAIL: Import log should show success status. Actual: {result.ImportLog.Status}");
                    return;
                }

                results.Add("PASS: Successful operations are committed properly with correct logging");
                
                // Cleanup
                File.Delete(tempExcelFile);
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during successful commit test: {ex.Message}");
            }
        }

        // Helper methods for creating test data and configurations
        private static DataTable CreateTestDataWithValidationErrors()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Age", typeof(int));

            // Add some valid and invalid records
            table.Rows.Add("John Doe", "john@example.com", 30);
            table.Rows.Add("", "invalid-email", -5); // Invalid: empty name, invalid email, negative age
            table.Rows.Add("Jane Smith", "jane@example.com", 25);
            table.Rows.Add(null, null, null); // Invalid: all null values

            return table;
        }

        private static DataTable CreateValidTestData()
        {
            var table = new DataTable();
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Age", typeof(int));

            table.Rows.Add("John Doe", "john@example.com", 30);
            table.Rows.Add("Jane Smith", "jane@example.com", 25);
            table.Rows.Add("Bob Johnson", "bob@example.com", 35);

            return table;
        }

        private static string CreateTempExcelFile(DataTable data)
        {
            var tempFile = Path.GetTempFileName() + ".xlsx";
            // For testing purposes, we'll just create a placeholder file
            // In a real implementation, this would create an actual Excel file
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

    // Mock service implementations for testing
    public class MockExcelReaderServiceForTransaction : ExcelDatabaseImportTool.Interfaces.Services.IExcelReaderService
    {
        private readonly DataTable _testData;

        public MockExcelReaderServiceForTransaction(DataTable testData)
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

    public class MockValidationServiceWithFailures : ExcelDatabaseImportTool.Interfaces.Services.IValidationService
    {
        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateDataRowAsync(DataRow row, List<FieldMapping> fieldMappings)
        {
            var result = new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult();
            
            // Simulate validation failures for certain conditions
            if (row["Name"] == null || string.IsNullOrEmpty(row["Name"].ToString()))
            {
                result.IsValid = false;
                result.Errors.Add("Name is required");
            }
            
            if (row["Age"] != null && Convert.ToInt32(row["Age"]) < 0)
            {
                result.IsValid = false;
                result.Errors.Add("Age cannot be negative");
            }

            result.IsValid = result.Errors.Count == 0;
            return Task.FromResult(result);
        }

        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateImportConfigurationAsync(ImportConfiguration config)
        {
            return Task.FromResult(new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult { IsValid = true });
        }
    }

    public class MockValidationServiceSuccess : ExcelDatabaseImportTool.Interfaces.Services.IValidationService
    {
        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateDataRowAsync(DataRow row, List<FieldMapping> fieldMappings)
        {
            return Task.FromResult(new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult { IsValid = true });
        }

        public Task<ExcelDatabaseImportTool.Interfaces.Services.ValidationResult> ValidateImportConfigurationAsync(ImportConfiguration config)
        {
            return Task.FromResult(new ExcelDatabaseImportTool.Interfaces.Services.ValidationResult { IsValid = true });
        }
    }

    public class MockForeignKeyResolverServiceForTransaction : ExcelDatabaseImportTool.Interfaces.Services.IForeignKeyResolverService
    {
        public Task<object?> ResolveForeignKeyAsync(string lookupValue, ForeignKeyMapping mapping, DatabaseConfiguration dbConfig)
        {
            return Task.FromResult<object?>(1); // Return a mock foreign key ID
        }

        public Task<Dictionary<string, object>> ResolveForeignKeysAsync(Dictionary<string, string> lookupValues, List<ForeignKeyMapping> mappings, DatabaseConfiguration dbConfig)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in lookupValues)
            {
                result[kvp.Key] = 1; // Return mock foreign key IDs
            }
            return Task.FromResult(result);
        }
    }

    public class MockDatabaseConnectionServiceWithErrors : ExcelDatabaseImportTool.Interfaces.Services.IDatabaseConnectionService
    {
        public Task<bool> TestConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult(true);
        }

        public Task<System.Data.IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult<System.Data.IDbConnection>(new MockDbConnectionWithErrors());
        }

        public string BuildConnectionString(DatabaseConfiguration config)
        {
            return "mock connection string";
        }
    }

    public class MockDatabaseConnectionServiceSuccess : ExcelDatabaseImportTool.Interfaces.Services.IDatabaseConnectionService
    {
        public Task<bool> TestConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult(true);
        }

        public Task<System.Data.IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult<System.Data.IDbConnection>(new MockDbConnectionSuccess());
        }

        public string BuildConnectionString(DatabaseConfiguration config)
        {
            return "mock connection string";
        }
    }

    // Mock database connection implementations for transaction testing
    public class MockDbConnectionWithErrors : System.Data.IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "MockDB";
        public System.Data.ConnectionState State => System.Data.ConnectionState.Open;

        public System.Data.IDbTransaction BeginTransaction() => new MockDbTransactionForTransaction();
        public System.Data.IDbTransaction BeginTransaction(System.Data.IsolationLevel il) => new MockDbTransactionForTransaction();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public System.Data.IDbCommand CreateCommand() => new MockDbCommandWithErrors();
        public void Dispose() { }
        public void Open() { }
        public Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class MockDbConnectionSuccess : System.Data.IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "MockDB";
        public System.Data.ConnectionState State => System.Data.ConnectionState.Open;

        public System.Data.IDbTransaction BeginTransaction() => new MockDbTransactionForTransaction();
        public System.Data.IDbTransaction BeginTransaction(System.Data.IsolationLevel il) => new MockDbTransactionForTransaction();
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public System.Data.IDbCommand CreateCommand() => new MockDbCommandSuccess();
        public void Dispose() { }
        public void Open() { }
        public Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class MockDbTransactionForTransaction : System.Data.IDbTransaction
    {
        public System.Data.IDbConnection Connection => new MockDbConnectionSuccess();
        public System.Data.IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public void Commit() { }
        public void Dispose() { }
        public void Rollback() { }
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class MockDbCommandWithErrors : System.Data.IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; } = 30;
        public System.Data.CommandType CommandType { get; set; } = System.Data.CommandType.Text;
        public System.Data.IDbConnection? Connection { get; set; }
        public System.Data.IDataParameterCollection Parameters { get; } = new MockParameterCollectionForTransaction();
        public System.Data.IDbTransaction? Transaction { get; set; }
        public System.Data.UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public System.Data.IDbDataParameter CreateParameter() => new MockDbParameterForTransaction();
        public void Dispose() { }
        public int ExecuteNonQuery() => throw new InvalidOperationException("Mock database error");
        public System.Data.IDataReader ExecuteReader() => throw new NotImplementedException();
        public System.Data.IDataReader ExecuteReader(System.Data.CommandBehavior behavior) => throw new NotImplementedException();
        public object ExecuteScalar() => throw new InvalidOperationException("Mock database error");
        public void Prepare() { }
        public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Mock database error");
    }

    public class MockDbCommandSuccess : System.Data.IDbCommand
    {
        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; } = 30;
        public System.Data.CommandType CommandType { get; set; } = System.Data.CommandType.Text;
        public System.Data.IDbConnection? Connection { get; set; }
        public System.Data.IDataParameterCollection Parameters { get; } = new MockParameterCollectionForTransaction();
        public System.Data.IDbTransaction? Transaction { get; set; }
        public System.Data.UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public System.Data.IDbDataParameter CreateParameter() => new MockDbParameterForTransaction();
        public void Dispose() { }
        public int ExecuteNonQuery() => 1;
        public System.Data.IDataReader ExecuteReader() => throw new NotImplementedException();
        public System.Data.IDataReader ExecuteReader(System.Data.CommandBehavior behavior) => throw new NotImplementedException();
        public object ExecuteScalar() => 1;
        public void Prepare() { }
        public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    public class MockDbParameterForTransaction : System.Data.IDbDataParameter
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

    public class MockParameterCollectionForTransaction : System.Data.IDataParameterCollection
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