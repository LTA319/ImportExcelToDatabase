using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Interfaces.Services;
using Moq;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 2: Connection testing consistency**
    /// **Validates: Requirements 1.3**
    /// </summary>
    [TestFixture]
    public class ConnectionTestingConsistencyTests
    {
        private Mock<IEncryptionService>? _mockEncryptionService;
        private DatabaseConnectionService? _connectionService;

        [SetUp]
        public void SetUp()
        {
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>()))
                                  .Returns<string>(encrypted => "decrypted_password");
            
            _connectionService = new DatabaseConnectionService(_mockEncryptionService.Object);
        }

        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool ConnectionStringBuildingConsistency(string name, int dbTypeInt, string server, 
            string database, string username, string encryptedPassword, int port)
        {
            // Filter out invalid inputs
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(encryptedPassword) || port <= 0 || port > 65535)
                return true; // Skip invalid inputs

            var dbType = Math.Abs(dbTypeInt % 2) == 0 ? DatabaseType.MySQL : DatabaseType.SqlServer;
            
            var config = new DatabaseConfiguration
            {
                Id = 1,
                Name = name,
                Type = dbType,
                Server = server,
                Database = database,
                Username = username,
                EncryptedPassword = encryptedPassword,
                Port = port,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            try
            {
                // Build connection string multiple times with same config
                var connectionString1 = _connectionService!.BuildConnectionString(config);
                var connectionString2 = _connectionService.BuildConnectionString(config);
                var connectionString3 = _connectionService.BuildConnectionString(config);
                
                // All connection strings should be identical and non-empty
                return connectionString1 == connectionString2 && 
                       connectionString2 == connectionString3 &&
                       !string.IsNullOrWhiteSpace(connectionString1);
            }
            catch (Exception)
            {
                // If an exception occurs, it should be consistent across calls
                try
                {
                    _connectionService!.BuildConnectionString(config);
                    return false; // Should have thrown again
                }
                catch
                {
                    return true; // Consistent exception behavior
                }
            }
        }

        [FsCheck.NUnit.Property(MaxTest = 50)]
        public bool ValidConfigurationProducesValidConnectionString()
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
                    var connectionString = _connectionService!.BuildConnectionString(config);
                    
                    // Valid configuration should produce non-empty connection string
                    if (string.IsNullOrWhiteSpace(connectionString))
                        return false;
                    
                    // Connection string should contain key components based on database type
                    var containsRequiredComponents = config.Type switch
                    {
                        DatabaseType.MySQL => connectionString.Contains("Server=") && 
                                             connectionString.Contains("Database=") &&
                                             connectionString.Contains("User ID="),
                        DatabaseType.SqlServer => connectionString.Contains("Data Source=") && 
                                                 connectionString.Contains("Initial Catalog=") &&
                                                 connectionString.Contains("User ID="),
                        _ => false
                    };
                    
                    if (!containsRequiredComponents)
                        return false;
                }
                catch (Exception)
                {
                    return false; // Valid config should not throw
                }
            }
            
            return true;
        }
    }
}