using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Models.Domain;

namespace ExcelDatabaseImportTool.Repositories
{
    public class ImportLogRepository : IImportLogRepository
    {
        private readonly ApplicationDbContext _context;

        public ImportLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportLog?> GetImportLogByIdAsync(int id)
        {
            return await _context.ImportLogs
                .Include(l => l.ImportConfiguration)
                .ThenInclude(i => i!.DatabaseConfiguration)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<ImportLog>> GetImportLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.ImportLogs
                .Include(l => l.ImportConfiguration)
                .ThenInclude(i => i!.DatabaseConfiguration)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.StartTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.StartTime <= toDate.Value);
            }

            return await query
                .OrderByDescending(l => l.StartTime)
                .ToListAsync();
        }

        public async Task<List<ImportLog>> GetImportLogsByConfigurationIdAsync(int configurationId)
        {
            return await _context.ImportLogs
                .Include(l => l.ImportConfiguration)
                .ThenInclude(i => i!.DatabaseConfiguration)
                .Where(l => l.ImportConfigurationId == configurationId)
                .OrderByDescending(l => l.StartTime)
                .ToListAsync();
        }

        public async Task SaveImportLogAsync(ImportLog log)
        {
            if (log.Id == 0)
            {
                _context.ImportLogs.Add(log);
            }
            else
            {
                _context.ImportLogs.Update(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateImportLogAsync(ImportLog log)
        {
            _context.ImportLogs.Update(log);
            await _context.SaveChangesAsync();
        }
    }
}