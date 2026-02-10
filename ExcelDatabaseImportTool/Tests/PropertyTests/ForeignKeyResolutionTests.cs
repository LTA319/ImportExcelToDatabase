using System.Data;
using System.IO;
using System.Collections;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Interfaces.Services;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 6: Foreign key resolution accuracy**
    /// **Validates: Requirements 2.4, 5.2**
    /// </summary>
    public static class ForeignKeyResolutionTests
    {
        public static void RunForeignKeyResolutionTests()
        {
            var results = new List<string>();
            results.Add("Running foreign key resolution accuracy tests...");

            try
            {
                // Test basic foreign key resolution
                TestBasicForeignKeyResolution(results);
                
                // Test caching mechanism
                TestCachingMechanism(results);
                
                // Test error handling for missing references
                TestMissingReferenceHandling(results);
                
                // Test edge cases
                TestEdgeCases(results);
                
                results.Add("Foreign key resolution accuracy tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during foreign key resolution tests: {ex.Message}");
            }
            
            // Write results to file
            File.WriteAllLines("foreign_key_resolution_test_results.txt", results);
        }

        private static void TestBasicForeignKeyResolution(List<string> results)
        {
            try
            {
                // Create mock database connection service
                var mockConnectionService = new MockDatabaseConnectionService();
                var foreignKeyResolver = new ForeignKeyResolverService(mockConnectionService);

                // Create test database configuration
                var dbConfig = new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "Test DB",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password"
                };

                // Create test foreign key mapping
                var mapping = new ForeignKeyMapping
                {
                    ReferencedTable = "Categories",
                    ReferencedLookupField = "CategoryName",
                    ReferencedKeyField = "CategoryId"
                };

                // Test cases for foreign key resolution
                var testCases = new[]
                {
                    new { LookupValue = "Electronics", ExpectedId = 1, Description = "Valid category lookup" },
                    new { LookupValue = "Books", ExpectedId = 2, Description = "Another valid category lookup" },
                    new { LookupValue = "Clothing", ExpectedId = 3, Description = "Third valid category lookup" }
                };

                foreach (var testCase in testCases)
                {
                    // Set up mock to return expected ID
                    mockConnectionService.SetMockResult(testCase.LookupValue, testCase.ExpectedId);

                    var result = foreignKeyResolver.ResolveForeignKeyAsync(testCase.LookupValue, mapping, dbConfig).Result;

                    if (result != null && result.Equals(testCase.ExpectedId))
                    {
                        results.Add($"PASS: Basic foreign key resolution - {testCase.Description}");
                    }
                    else
                    {
                        results.Add($"FAIL: Basic foreign key resolution - {testCase.Description}");
                        results.Add($"  Expected: {testCase.ExpectedId}, Actual: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during basic foreign key resolution test: {ex.Message}");
            }
        }

        private static void TestCachingMechanism(List<string> results)
        {
            try
            {
                var mockConnectionService = new MockDatabaseConnectionService();
                var foreignKeyResolver = new ForeignKeyResolverService(mockConnectionService);

                var dbConfig = new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "Test DB",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password"
                };

                var mapping = new ForeignKeyMapping
                {
                    ReferencedTable = "Categories",
                    ReferencedLookupField = "CategoryName",
                    ReferencedKeyField = "CategoryId"
                };

                // Set up mock to return a value and track call count
                mockConnectionService.SetMockResult("Electronics", 1);
                mockConnectionService.ResetCallCount();

                // First call should hit the database
                var result1 = foreignKeyResolver.ResolveForeignKeyAsync("Electronics", mapping, dbConfig).Result;
                var callCountAfterFirst = mockConnectionService.GetCallCount();

                // Second call should use cache
                var result2 = foreignKeyResolver.ResolveForeignKeyAsync("Electronics", mapping, dbConfig).Result;
                var callCountAfterSecond = mockConnectionService.GetCallCount();

                if (result1 != null && result1.Equals(1) && result2 != null && result2.Equals(1))
                {
                    if (callCountAfterFirst == 1 && callCountAfterSecond == 1)
                    {
                        results.Add("PASS: Caching mechanism - Second call used cache");
                    }
                    else
                    {
                        results.Add("FAIL: Caching mechanism - Second call did not use cache");
                        results.Add($"  Call count after first: {callCountAfterFirst}, after second: {callCountAfterSecond}");
                    }
                }
                else
                {
                    results.Add("FAIL: Caching mechanism - Results don't match");
                    results.Add($"  Result1: {result1}, Result2: {result2}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during caching mechanism test: {ex.Message}");
            }
        }

        private static void TestMissingReferenceHandling(List<string> results)
        {
            try
            {
                var mockConnectionService = new MockDatabaseConnectionService();
                var foreignKeyResolver = new ForeignKeyResolverService(mockConnectionService);

                var dbConfig = new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "Test DB",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password"
                };

                var mapping = new ForeignKeyMapping
                {
                    ReferencedTable = "Categories",
                    ReferencedLookupField = "CategoryName",
                    ReferencedKeyField = "CategoryId"
                };

                // Set up mock to return null for non-existent value
                mockConnectionService.SetMockResult("NonExistentCategory", null);

                var result = foreignKeyResolver.ResolveForeignKeyAsync("NonExistentCategory", mapping, dbConfig).Result;

                if (result == null)
                {
                    results.Add("PASS: Missing reference handling - Null returned for non-existent value");
                }
                else
                {
                    results.Add("FAIL: Missing reference handling - Non-null returned for non-existent value");
                    results.Add($"  Result: {result}");
                }

                // Test empty/null lookup values
                var emptyResult = foreignKeyResolver.ResolveForeignKeyAsync("", mapping, dbConfig).Result;
                var nullResult = foreignKeyResolver.ResolveForeignKeyAsync(null, mapping, dbConfig).Result;

                if (emptyResult == null && nullResult == null)
                {
                    results.Add("PASS: Missing reference handling - Null returned for empty/null lookup values");
                }
                else
                {
                    results.Add("FAIL: Missing reference handling - Non-null returned for empty/null lookup values");
                    results.Add($"  Empty result: {emptyResult}, Null result: {nullResult}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during missing reference handling test: {ex.Message}");
            }
        }

        private static void TestEdgeCases(List<string> results)
        {
            try
            {
                var mockConnectionService = new MockDatabaseConnectionService();
                var foreignKeyResolver = new ForeignKeyResolverService(mockConnectionService);

                var dbConfig = new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "Test DB",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password"
                };

                var mapping = new ForeignKeyMapping
                {
                    ReferencedTable = "Categories",
                    ReferencedLookupField = "CategoryName",
                    ReferencedKeyField = "CategoryId"
                };

                // Test null parameters
                try
                {
                    var nullMappingResult = foreignKeyResolver.ResolveForeignKeyAsync("Test", null, dbConfig).Result;
                    results.Add("FAIL: Edge case - Null mapping should throw exception");
                }
                catch (ArgumentNullException)
                {
                    results.Add("PASS: Edge case - Null mapping correctly throws ArgumentNullException");
                }
                catch (AggregateException ex) when (ex.InnerException is ArgumentNullException)
                {
                    results.Add("PASS: Edge case - Null mapping correctly throws ArgumentNullException");
                }

                try
                {
                    var nullDbConfigResult = foreignKeyResolver.ResolveForeignKeyAsync("Test", mapping, null).Result;
                    results.Add("FAIL: Edge case - Null database config should throw exception");
                }
                catch (ArgumentNullException)
                {
                    results.Add("PASS: Edge case - Null database config correctly throws ArgumentNullException");
                }
                catch (AggregateException ex) when (ex.InnerException is ArgumentNullException)
                {
                    results.Add("PASS: Edge case - Null database config correctly throws ArgumentNullException");
                }

                // Test whitespace-only lookup value
                var whitespaceResult = foreignKeyResolver.ResolveForeignKeyAsync("   ", mapping, dbConfig).Result;
                if (whitespaceResult == null)
                {
                    results.Add("PASS: Edge case - Whitespace-only lookup value returns null");
                }
                else
                {
                    results.Add("FAIL: Edge case - Whitespace-only lookup value should return null");
                    results.Add($"  Result: {whitespaceResult}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during edge case test: {ex.Message}");
            }
        }
    }

    // Mock database connection service for testing
    public class MockDatabaseConnectionService : IDatabaseConnectionService
    {
        private readonly Dictionary<string, object?> _mockResults = new();
        private int _callCount = 0;

        public void SetMockResult(string lookupValue, object? result)
        {
            _mockResults[lookupValue] = result;
        }

        public void ResetCallCount()
        {
            _callCount = 0;
        }

        public int GetCallCount()
        {
            return _callCount;
        }

        public Task<IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
        {
            _callCount++;
            return Task.FromResult<IDbConnection>(new MockDbConnection(_mockResults));
        }

        public Task<bool> TestConnectionAsync(DatabaseConfiguration config)
        {
            return Task.FromResult(true);
        }

        public string BuildConnectionString(DatabaseConfiguration config)
        {
            return "mock_connection_string";
        }
    }

    // Mock database connection for testing
    public class MockDbConnection : IDbConnection
    {
        private readonly Dictionary<string, object?> _mockResults;

        public MockDbConnection(Dictionary<string, object?> mockResults)
        {
            _mockResults = mockResults;
            State = ConnectionState.Open;
        }

        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "mockdb";
        public ConnectionState State { get; private set; }

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() => State = ConnectionState.Closed;

        public IDbCommand CreateCommand()
        {
            return new MockDbCommand(_mockResults);
        }

        public void Dispose() => State = ConnectionState.Closed;
        public void Open() => State = ConnectionState.Open;
    }

    // Mock database command for testing
    public class MockDbCommand : IDbCommand
    {
        private readonly Dictionary<string, object?> _mockResults;

        public MockDbCommand(Dictionary<string, object?> mockResults)
        {
            _mockResults = mockResults;
            Parameters = new MockParameterCollection();
        }

        public string CommandText { get; set; } = "";
        public int CommandTimeout { get; set; } = 30;
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDbConnection? Connection { get; set; }
        public IDataParameterCollection Parameters { get; }
        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() => throw new NotImplementedException();

        public IDbDataParameter CreateParameter()
        {
            return new MockDbParameter();
        }

        public void Dispose() { }

        public int ExecuteNonQuery() => throw new NotImplementedException();

        public IDataReader ExecuteReader() => throw new NotImplementedException();
        public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotImplementedException();

        public object? ExecuteScalar()
        {
            // Extract the lookup value from the parameter
            var parameter = Parameters.Cast<MockDbParameter>().FirstOrDefault(p => p.ParameterName == "@lookupValue");
            if (parameter?.Value != null)
            {
                var lookupValue = parameter.Value.ToString();
                if (lookupValue != null && _mockResults.TryGetValue(lookupValue, out var result))
                {
                    return result;
                }
            }
            return null;
        }

        public void Prepare() => throw new NotImplementedException();
    }

    // Mock parameter collection for testing
    public class MockParameterCollection : IDataParameterCollection
    {
        private readonly List<IDataParameter> _parameters = new();

        public object? this[string parameterName] 
        { 
            get => _parameters.FirstOrDefault(p => p.ParameterName == parameterName);
            set => throw new NotImplementedException();
        }
        public object? this[int index] 
        { 
            get => _parameters[index];
            set => throw new NotImplementedException();
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public int Count => _parameters.Count;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public int Add(object? value)
        {
            if (value is IDataParameter param)
            {
                _parameters.Add(param);
                return _parameters.Count - 1;
            }
            throw new ArgumentException("Value must be IDataParameter");
        }

        public void Clear() => _parameters.Clear();
        public bool Contains(object? value) => value != null && _parameters.Contains(value);
        public bool Contains(string parameterName) => _parameters.Any(p => p.ParameterName == parameterName);
        public void CopyTo(Array array, int index) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public int IndexOf(object? value) => value != null ? _parameters.IndexOf((IDataParameter)value) : -1;
        public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public void Insert(int index, object? value) => throw new NotImplementedException();
        public void Remove(object? value) { if (value != null) _parameters.Remove((IDataParameter)value); }
        public void RemoveAt(int index) => _parameters.RemoveAt(index);
        public void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
    }

    // Mock parameter for testing
    public class MockDbParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable => true;
        public string ParameterName { get; set; } = "";
        public string SourceColumn { get; set; } = "";
        public DataRowVersion SourceVersion { get; set; }
        public object? Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }
}