using System.Data;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;

namespace ExcelDatabaseImportTool.Services.Database
{
    public class DatabaseConnectionService : IDatabaseConnectionService
    {
        private readonly IEncryptionService _encryptionService;
        private const int DefaultConnectionTimeoutSeconds = 30;

        public DatabaseConnectionService(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        public async Task<IDbConnection> CreateConnectionAsync(DatabaseConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var connectionString = BuildConnectionString(config);
            
            IDbConnection connection = config.Type switch
            {
                DatabaseType.MySQL => new MySqlConnection(connectionString),
                DatabaseType.SqlServer => new SqlConnection(connectionString),
                _ => throw new NotSupportedException($"Database type {config.Type} is not supported")
            };

            try
            {
                // Open connection using the specific connection type's async method
                switch (connection)
                {
                    case MySqlConnection mysqlConn:
                        await mysqlConn.OpenAsync();
                        break;
                    case SqlConnection sqlConn:
                        await sqlConn.OpenAsync();
                        break;
                    default:
                        connection.Open(); // Fallback to synchronous open
                        break;
                }
                
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync(DatabaseConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                using var connection = await CreateConnectionAsync(config);
                return connection.State == ConnectionState.Open;
            }
            catch (InvalidOperationException)
            {
                // Re-throw password decryption errors so they can be shown to the user
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception details for debugging but return false for test failure
                System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        public string BuildConnectionString(DatabaseConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(config.EncryptedPassword))
                throw new ArgumentException("Encrypted password cannot be null or empty", nameof(config));

            string decryptedPassword;
            try
            {
                decryptedPassword = _encryptionService.Decrypt(config.EncryptedPassword);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to decrypt password for database configuration '{config.Name}'. " +
                    "The stored password may be corrupted or in an invalid format. " +
                    "Please re-enter the password and save the configuration again.", ex);
            }

            return config.Type switch
            {
                DatabaseType.MySQL => BuildMySqlConnectionString(config, decryptedPassword),
                DatabaseType.SqlServer => BuildSqlServerConnectionString(config, decryptedPassword),
                _ => throw new NotSupportedException($"Database type {config.Type} is not supported")
            };
        }

        private string BuildMySqlConnectionString(DatabaseConfiguration config, string password)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = config.Server,
                Database = config.Database,
                UserID = config.Username,
                Password = password,
                Port = (uint)(config.Port > 0 ? config.Port : 3306),
                ConnectionTimeout = (uint)DefaultConnectionTimeoutSeconds,
                DefaultCommandTimeout = (uint)DefaultConnectionTimeoutSeconds,
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 10,
                SslMode = MySqlSslMode.Preferred
            };

            return builder.ConnectionString;
        }

        private string BuildSqlServerConnectionString(DatabaseConfiguration config, string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = config.Port > 0 ? $"{config.Server},{config.Port}" : config.Server,
                InitialCatalog = config.Database,
                UserID = config.Username,
                Password = password,
                ConnectTimeout = DefaultConnectionTimeoutSeconds,
                CommandTimeout = DefaultConnectionTimeoutSeconds,
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 10,
                TrustServerCertificate = true,
                Encrypt = true
            };

            return builder.ConnectionString;
        }
    }
}