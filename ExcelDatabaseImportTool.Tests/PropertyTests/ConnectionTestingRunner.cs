using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Interfaces.Services;
using Moq;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 2: Connection testing consistency**
    /// **Validates: Requirements 1.3**
    /// </summary>
    public static class ConnectionTestingRunner
    {
        public static void RunConnectionTestingConsistencyTests()
        {
            var results = new List<string>();
            results.Add("Running connection testing consistency tests...");

            try
            {
                var mockEncryptionService = new Mock<IEncryptionService>();
                mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>()))
                                    .Returns<string>(encrypted => "decrypted_password");
                
                var connectionService = new DatabaseConnectionService(mockEncryptionService.Object);

                // Test connection string building consistency
                TestConnectionStringBuildingConsistency(connectionService, results);
                
                // Test valid configuration produces valid connection string
                TestValidConfigurationProducesValidConnectionString(connectionService, results);
                
                results.Add("Connection testing consistency tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during connection testing consistency tests: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }
            
            // Write results to file
            File.WriteAllLines("connection_testing_test_results.txt", results);
        }

        private static void TestConnectionStringBuildingConsistency(DatabaseConnectionService connectionService, List<string> results)
        {
            var testConfigurations = new[]
            {
                new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "MySQL Test Config",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password_123",
                    Port = 3306,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                },
                new DatabaseConfiguration
                {
                    Id = 2,
                    Name = "SQL Server Test Config",
                    Type = DatabaseType.SqlServer,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password_456",
                    Port = 1433,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                }
            };

            foreach (var config in testConfigurations)
            {
                try
                {
                    // Build connection string multiple times with same config
                    var connectionString1 = connectionService.BuildConnectionString(config);
                    var connectionString2 = connectionService.BuildConnectionString(config);
                    var connectionString3 = connectionService.BuildConnectionString(config);
                    
                    // All connection strings should be identical and non-empty
                    var isConsistent = connectionString1 == connectionString2 && 
                                      connectionString2 == connectionString3 &&
                                      !string.IsNullOrWhiteSpace(connectionString1);

                    if (!isConsistent)
                    {
                        results.Add($"FAIL: Connection string building not consistent for: {config.Name}");
                        results.Add($"  String 1: {connectionString1}");
                        results.Add($"  String 2: {connectionString2}");
                        results.Add($"  String 3: {connectionString3}");
                        continue;
                    }

                    results.Add($"PASS: Connection string building consistent for: {config.Name}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception during connection string consistency test for '{config.Name}': {ex.Message}");
                }
            }
        }

        private static void TestValidConfigurationProducesValidConnectionString(DatabaseConnectionService connectionService, List<string> results)
        {
            var validConfigs = new[]
            {
                new DatabaseConfiguration
                {
                    Id = 1,
                    Name = "TestMySQL",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_pass",
                    Port = 3306,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                },
                new DatabaseConfiguration
                {
                    Id = 2,
                    Name = "TestSqlServer",
                    Type = DatabaseType.SqlServer,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_pass",
                    Port = 1433,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                }
            };

            foreach (var config in validConfigs)
            {
                try
                {
                    var connectionString = connectionService.BuildConnectionString(config);
                    
                    // Valid configuration should produce non-empty connection string
                    if (string.IsNullOrWhiteSpace(connectionString))
                    {
                        results.Add($"FAIL: Valid configuration produced empty connection string for: {config.Name}");
                        continue;
                    }
                    
                    // Connection string should contain key components based on database type
                    var containsRequiredComponents = config.Type switch
                    {
                        DatabaseType.MySQL => connectionString.Contains("server=", StringComparison.OrdinalIgnoreCase) && 
                                             connectionString.Contains("database=", StringComparison.OrdinalIgnoreCase) &&
                                             connectionString.Contains("user id=", StringComparison.OrdinalIgnoreCase),
                        DatabaseType.SqlServer => connectionString.Contains("Data Source=") && 
                                                 connectionString.Contains("Initial Catalog=") &&
                                                 connectionString.Contains("User ID="),
                        _ => false
                    };
                    
                    if (!containsRequiredComponents)
                    {
                        results.Add($"FAIL: Connection string missing required components for: {config.Name}");
                        results.Add($"  Connection string: {connectionString}");
                        continue;
                    }

                    results.Add($"PASS: Valid configuration produces valid connection string for: {config.Name}");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Valid config should not throw exception for '{config.Name}': {ex.Message}");
                }
            }
        }
    }
}