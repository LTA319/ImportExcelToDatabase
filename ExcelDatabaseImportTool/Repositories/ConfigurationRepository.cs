using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Repositories
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly ApplicationDbContext _context;

        public ConfigurationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DatabaseConfiguration>> GetDatabaseConfigurationsAsync()
        {
            return await _context.DatabaseConfigurations
                .Include(d => d.ImportConfigurations)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<DatabaseConfiguration?> GetDatabaseConfigurationByIdAsync(int id)
        {
            return await _context.DatabaseConfigurations
                .Include(d => d.ImportConfigurations)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config)
        {
            if (config.Id == 0)
            {
                config.CreatedDate = DateTime.UtcNow;
                config.ModifiedDate = DateTime.UtcNow;
                _context.DatabaseConfigurations.Add(config);
            }
            else
            {
                config.ModifiedDate = DateTime.UtcNow;
                _context.DatabaseConfigurations.Update(config);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsDatabaseConfigurationReferencedAsync(int id)
        {
            return await _context.ImportConfigurations
                .AnyAsync(i => i.DatabaseConfigurationId == id);
        }

        public async Task DeleteDatabaseConfigurationAsync(int id)
        {
            var config = await _context.DatabaseConfigurations.FindAsync(id);
            if (config != null)
            {
                _context.DatabaseConfigurations.Remove(config);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ImportConfiguration>> GetImportConfigurationsAsync()
        {
            return await _context.ImportConfigurations
                .Include(i => i.DatabaseConfiguration)
                .Include(i => i.ImportLogs)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<ImportConfiguration?> GetImportConfigurationByIdAsync(int id)
        {
            return await _context.ImportConfigurations
                .Include(i => i.DatabaseConfiguration)
                .Include(i => i.ImportLogs)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task SaveImportConfigurationAsync(ImportConfiguration config)
        {
            if (config.Id == 0)
            {
                config.CreatedDate = DateTime.UtcNow;
                config.ModifiedDate = DateTime.UtcNow;
                _context.ImportConfigurations.Add(config);
            }
            else
            {
                config.ModifiedDate = DateTime.UtcNow;
                _context.ImportConfigurations.Update(config);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteImportConfigurationAsync(int id)
        {
            var config = await _context.ImportConfigurations.FindAsync(id);
            if (config != null)
            {
                _context.ImportConfigurations.Remove(config);
                await _context.SaveChangesAsync();
            }
        }
    }
}