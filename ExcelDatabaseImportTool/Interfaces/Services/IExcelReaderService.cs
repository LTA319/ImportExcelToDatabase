using System.Data;

namespace ExcelDatabaseImportTool.Interfaces.Services
{
    public interface IExcelReaderService
    {
        Task<DataTable> ReadExcelFileAsync(string filePath);
        Task<List<string>> GetColumnNamesAsync(string filePath);
        Task<bool> ValidateFileAsync(string filePath);
    }
}