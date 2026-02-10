using System.Data;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Interfaces.Services
{
    public interface IDatabaseConnectionService
    {
        Task<bool> TestConnectionAsync(DatabaseConfiguration config);
        Task<IDbConnection> CreateConnectionAsync(DatabaseConfiguration config);
        string BuildConnectionString(DatabaseConfiguration config);
    }
}