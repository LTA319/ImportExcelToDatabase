using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExcelDatabaseImportTool.Data.Context;
using System.IO;

namespace ExcelDatabaseImportTool.Services.Database
{
    public interface IDatabaseInitializationService
    {
        Task InitializeDatabaseAsync();
        Task MigrateDatabaseAsync();
        Task<bool> DatabaseExistsAsync();
        Task<bool> BackupDatabaseAsync(string? backupPath = null);
        Task<bool> RestoreDatabaseAsync(string backupPath);
        Task<int> GetDatabaseVersionAsync();
    }

    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseInitializationService> _logger;
        private const int CurrentDatabaseVersion = 1;
        private const string BackupDirectory = "Backups";

        public DatabaseInitializationService(
            ApplicationDbContext context,
            ILogger<DatabaseInitializationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Initializing database...");

                // Create backup directory if it doesn't exist
                EnsureBackupDirectoryExists();

                // Check if database exists
                var exists = await DatabaseExistsAsync();

                if (!exists)
                {
                    _logger.LogInformation("Creating new database...");
                    await _context.Database.EnsureCreatedAsync();
                    _logger.LogInformation("Database created successfully");
                }
                else
                {
                    _logger.LogInformation("Database already exists");
                    
                    // Check database version and upgrade if needed
                    await CheckAndUpgradeDatabaseAsync();
                }

                // Apply any pending migrations
                await MigrateDatabaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw new InvalidOperationException("Failed to initialize database", ex);
            }
        }

        public async Task MigrateDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Checking for pending migrations...");
                
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"Found {pendingMigrations.Count()} pending migrations");
                    
                    // Create backup before migration
                    var backupSuccess = await BackupDatabaseAsync();
                    if (!backupSuccess)
                    {
                        _logger.LogWarning("Failed to create backup before migration, proceeding anyway");
                    }

                    // Apply migrations
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply database migrations");
                throw new InvalidOperationException("Failed to apply database migrations", ex);
            }
        }

        public async Task<bool> DatabaseExistsAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if database exists");
                return false;
            }
        }

        public async Task<bool> BackupDatabaseAsync(string? backupPath = null)
        {
            try
            {
                // Get the database file path
                var connectionString = _context.Database.GetConnectionString();
                var dbFilePath = ExtractDatabasePathFromConnectionString(connectionString);

                if (string.IsNullOrEmpty(dbFilePath) || !File.Exists(dbFilePath))
                {
                    _logger.LogWarning("Database file not found, cannot create backup");
                    return false;
                }

                // Ensure all changes are saved before backup
                await _context.SaveChangesAsync();

                // Generate backup path if not provided
                if (string.IsNullOrEmpty(backupPath))
                {
                    EnsureBackupDirectoryExists();
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    backupPath = Path.Combine(BackupDirectory, $"ExcelImportTool_backup_{timestamp}.db");
                }

                // Create backup
                _logger.LogInformation($"Creating database backup at: {backupPath}");
                File.Copy(dbFilePath, backupPath, overwrite: true);
                _logger.LogInformation("Database backup created successfully");

                // Clean up old backups (keep last 10)
                await CleanupOldBackupsAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database backup");
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    _logger.LogError($"Backup file not found: {backupPath}");
                    return false;
                }

                var connectionString = _context.Database.GetConnectionString();
                var dbFilePath = ExtractDatabasePathFromConnectionString(connectionString);

                if (string.IsNullOrEmpty(dbFilePath))
                {
                    _logger.LogError("Could not determine database file path");
                    return false;
                }

                _logger.LogInformation($"Restoring database from backup: {backupPath}");

                // Close all connections
                await _context.Database.CloseConnectionAsync();

                // Restore the backup
                File.Copy(backupPath, dbFilePath, overwrite: true);
                
                _logger.LogInformation("Database restored successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore database from backup");
                return false;
            }
        }

        public async Task<int> GetDatabaseVersionAsync()
        {
            try
            {
                // For now, return current version
                // In a real implementation, you would query a version table
                return CurrentDatabaseVersion;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get database version");
                return 0;
            }
        }

        private async Task CheckAndUpgradeDatabaseAsync()
        {
            try
            {
                var currentVersion = await GetDatabaseVersionAsync();
                
                if (currentVersion < CurrentDatabaseVersion)
                {
                    _logger.LogInformation($"Database upgrade needed from version {currentVersion} to {CurrentDatabaseVersion}");
                    
                    // Create backup before upgrade
                    var backupSuccess = await BackupDatabaseAsync();
                    if (!backupSuccess)
                    {
                        _logger.LogWarning("Failed to create backup before upgrade");
                    }

                    // Perform upgrade logic here
                    // For now, we'll just log that an upgrade would happen
                    _logger.LogInformation("Database upgrade completed");
                }
                else
                {
                    _logger.LogInformation($"Database is at current version {currentVersion}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check and upgrade database");
                throw;
            }
        }

        private void EnsureBackupDirectoryExists()
        {
            if (!Directory.Exists(BackupDirectory))
            {
                Directory.CreateDirectory(BackupDirectory);
                _logger.LogInformation($"Created backup directory: {BackupDirectory}");
            }
        }

        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(BackupDirectory, "ExcelImportTool_backup_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Keep only the last 10 backups
                var filesToDelete = backupFiles.Skip(10);
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        _logger.LogInformation($"Deleted old backup: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete old backup: {file.Name}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup old backups");
            }
        }

        private string? ExtractDatabasePathFromConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;

            // Parse SQLite connection string to get Data Source
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2 && 
                    keyValue[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    return keyValue[1].Trim();
                }
            }

            return null;
        }
    }
}