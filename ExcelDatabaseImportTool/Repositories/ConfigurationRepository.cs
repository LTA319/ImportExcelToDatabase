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
                .AsNoTracking()
                .Include(d => d.ImportConfigurations)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<DatabaseConfiguration?> GetDatabaseConfigurationByIdAsync(int id)
        {
            return await _context.DatabaseConfigurations
                .AsNoTracking()
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
                .AsNoTracking()
                .Include(i => i.DatabaseConfiguration)
                .Include(i => i.ImportLogs)
                .Include(i => i.FieldMappings)
                    .ThenInclude(f => f.ForeignKeyMapping)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }

        public async Task<ImportConfiguration?> GetImportConfigurationByIdAsync(int id)
        {
            return await _context.ImportConfigurations
                .AsNoTracking()
                .Include(i => i.DatabaseConfiguration)
                .Include(i => i.ImportLogs)
                .Include(i => i.FieldMappings)
                    .ThenInclude(f => f.ForeignKeyMapping)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task SaveImportConfigurationAsync(ImportConfiguration config)
        {
            if (config.Id == 0)
            {
                // New configuration - just add it
                config.CreatedDate = DateTime.UtcNow;
                config.ModifiedDate = DateTime.UtcNow;
                _context.ImportConfigurations.Add(config);
            }
            else
            {
                // Existing configuration - need to handle related entities properly
                config.ModifiedDate = DateTime.UtcNow;
                
                // Load the existing configuration with its field mappings
                var existingConfig = await _context.ImportConfigurations
                    .Include(i => i.FieldMappings)
                        .ThenInclude(f => f.ForeignKeyMapping)
                    .FirstOrDefaultAsync(i => i.Id == config.Id);

                if (existingConfig != null)
                {
                    // Update scalar properties
                    existingConfig.Name = config.Name;
                    existingConfig.DatabaseConfigurationId = config.DatabaseConfigurationId;
                    existingConfig.TableName = config.TableName;
                    existingConfig.HasHeaderRow = config.HasHeaderRow;
                    existingConfig.ModifiedDate = config.ModifiedDate;

                    // Remove field mappings that are no longer present
                    var mappingsToRemove = existingConfig.FieldMappings
                        .Where(existing => !config.FieldMappings.Any(updated => updated.Id == existing.Id))
                        .ToList();
                    
                    foreach (var mapping in mappingsToRemove)
                    {
                        _context.FieldMappings.Remove(mapping);
                    }

                    // Update or add field mappings
                    foreach (var updatedMapping in config.FieldMappings)
                    {
                        var existingMapping = existingConfig.FieldMappings
                            .FirstOrDefault(m => m.Id == updatedMapping.Id);

                        if (existingMapping != null)
                        {
                            // Update existing mapping
                            existingMapping.ExcelColumnName = updatedMapping.ExcelColumnName;
                            existingMapping.DatabaseFieldName = updatedMapping.DatabaseFieldName;
                            existingMapping.IsRequired = updatedMapping.IsRequired;
                            existingMapping.DataType = updatedMapping.DataType;

                            // Handle foreign key mapping
                            if (updatedMapping.ForeignKeyMapping != null)
                            {
                                if (existingMapping.ForeignKeyMapping != null)
                                {
                                    // Update existing foreign key mapping
                                    existingMapping.ForeignKeyMapping.ReferencedTable = updatedMapping.ForeignKeyMapping.ReferencedTable;
                                    existingMapping.ForeignKeyMapping.ReferencedLookupField = updatedMapping.ForeignKeyMapping.ReferencedLookupField;
                                    existingMapping.ForeignKeyMapping.ReferencedKeyField = updatedMapping.ForeignKeyMapping.ReferencedKeyField;
                                }
                                else
                                {
                                    // Add new foreign key mapping
                                    existingMapping.ForeignKeyMapping = updatedMapping.ForeignKeyMapping;
                                }
                            }
                            else if (existingMapping.ForeignKeyMapping != null)
                            {
                                // Remove foreign key mapping
                                _context.ForeignKeyMappings.Remove(existingMapping.ForeignKeyMapping);
                                existingMapping.ForeignKeyMapping = null;
                                existingMapping.ForeignKeyMappingId = null;
                            }
                        }
                        else
                        {
                            // Add new mapping
                            updatedMapping.ImportConfigurationId = existingConfig.Id;
                            existingConfig.FieldMappings.Add(updatedMapping);
                        }
                    }
                }
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