using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Services.Import
{
    public class ImportService : IImportService
    {
        public event EventHandler<ImportProgressEventArgs>? ProgressUpdated;

        public Task<ImportResult> ImportDataAsync(ImportConfiguration config, string excelFilePath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Will be implemented in task 6.1");
        }
    }
}