using ExcelDatabaseImportTool.Models.Domain;

namespace ExcelDatabaseImportTool.Interfaces.Repositories
{
    public interface IImportLogRepository
    {
        Task SaveImportLogAsync(ImportLog log);
        Task UpdateImportLogAsync(ImportLog log);
        Task<List<ImportLog>> GetImportLogsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<ImportLog?> GetImportLogByIdAsync(int id);
        Task<List<ImportLog>> GetImportLogsByConfigurationIdAsync(int configurationId);
    }
}