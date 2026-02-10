using System.Data;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Interfaces.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public interface IValidationService
    {
        Task<ValidationResult> ValidateDataRowAsync(DataRow row, List<FieldMapping> fieldMappings);
        Task<ValidationResult> ValidateImportConfigurationAsync(ImportConfiguration config);
    }
}