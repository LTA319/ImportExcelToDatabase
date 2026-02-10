using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;

namespace ExcelDatabaseImportTool.Interfaces.Services
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public ImportLog ImportLog { get; set; } = new ImportLog();
    }

    public class ImportProgressEventArgs : EventArgs
    {
        public int ProcessedRecords { get; set; }
        public int TotalRecords { get; set; }
        public string CurrentOperation { get; set; } = string.Empty;
        public bool CanCancel { get; set; }
    }

    public interface IImportService
    {
        Task<ImportResult> ImportDataAsync(ImportConfiguration config, string excelFilePath, CancellationToken cancellationToken = default);
        event EventHandler<ImportProgressEventArgs>? ProgressUpdated;
    }
}