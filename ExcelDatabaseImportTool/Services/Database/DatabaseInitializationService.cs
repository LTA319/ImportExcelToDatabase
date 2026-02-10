using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;

namespace ExcelDatabaseImportTool.Services.Database
{
    public interface IDatabaseInitializationService
    {
        Task InitializeDatabaseAsync();
        Task MigrateDatabaseAsync();
        Task<bool> DatabaseExistsAsync();
    }

    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly ApplicationDbContext _context;

        public DatabaseInitializationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Apply any pending migrations
                await MigrateDatabaseAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize database", ex);
            }
        }

        public async Task MigrateDatabaseAsync()
        {
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to apply database migrations", ex);
            }
        }

        public async Task<bool> DatabaseExistsAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}