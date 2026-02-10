using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Interfaces.Services
{
    public interface IForeignKeyResolverService
    {
        Task<object?> ResolveForeignKeyAsync(string lookupValue, ForeignKeyMapping mapping, DatabaseConfiguration dbConfig);
        Task<Dictionary<string, object>> ResolveForeignKeysAsync(Dictionary<string, string> lookupValues, List<ForeignKeyMapping> mappings, DatabaseConfiguration dbConfig);
    }
}