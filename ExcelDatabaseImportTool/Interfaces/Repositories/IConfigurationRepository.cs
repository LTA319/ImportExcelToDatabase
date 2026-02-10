using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Interfaces.Repositories
{
    public interface IConfigurationRepository
    {
        Task<List<DatabaseConfiguration>> GetDatabaseConfigurationsAsync();
        Task<DatabaseConfiguration?> GetDatabaseConfigurationByIdAsync(int id);
        Task<List<ImportConfiguration>> GetImportConfigurationsAsync();
        Task<ImportConfiguration?> GetImportConfigurationByIdAsync(int id);
        Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config);
        Task SaveImportConfigurationAsync(ImportConfiguration config);
        Task DeleteDatabaseConfigurationAsync(int id);
        Task DeleteImportConfigurationAsync(int id);
        Task<bool> IsDatabaseConfigurationReferencedAsync(int id);
    }
}