using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExcelDatabaseImportTool.Services.Logging
{
    /// <summary>
    /// Manages log files including rotation and cleanup
    /// </summary>
    public class LogFileManager
    {
        private readonly ILogger<LogFileManager> _logger;
        private readonly string _logDirectory;
        private readonly int _maxLogFiles;
        private readonly long _maxLogSizeBytes;

        public LogFileManager(ILogger<LogFileManager> logger, int maxLogFiles = 30, long maxLogSizeMB = 100)
        {
            _logger = logger;
            _maxLogFiles = maxLogFiles;
            _maxLogSizeBytes = maxLogSizeMB * 1024 * 1024;
            
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// Gets all log files sorted by date (newest first)
        /// </summary>
        public List<LogFileInfo> GetLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new LogFileInfo
                    {
                        FilePath = f.FullName,
                        FileName = f.Name,
                        Size = f.Length,
                        LastModified = f.LastWriteTime,
                        FormattedSize = FormatBytes(f.Length)
                    })
                    .ToList();

                return logFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log files");
                return new List<LogFileInfo>();
            }
        }

        /// <summary>
        /// Cleans up old log files based on retention policy
        /// </summary>
        public async Task<int> CleanupOldLogsAsync()
        {
            try
            {
                var logFiles = GetLogFiles();
                var deletedCount = 0;

                // Delete files exceeding max count
                if (logFiles.Count > _maxLogFiles)
                {
                    var filesToDelete = logFiles.Skip(_maxLogFiles).ToList();
                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            File.Delete(file.FilePath);
                            deletedCount++;
                            _logger.LogInformation("Deleted old log file: {FileName}", file.FileName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete log file: {FileName}", file.FileName);
                        }
                    }
                }

                // Check total size and delete oldest if exceeding limit
                var totalSize = logFiles.Sum(f => f.Size);
                if (totalSize > _maxLogSizeBytes)
                {
                    var sortedByDate = logFiles.OrderBy(f => f.LastModified).ToList();
                    foreach (var file in sortedByDate)
                    {
                        if (totalSize <= _maxLogSizeBytes)
                            break;

                        try
                        {
                            File.Delete(file.FilePath);
                            totalSize -= file.Size;
                            deletedCount++;
                            _logger.LogInformation("Deleted log file to reduce total size: {FileName}", file.FileName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete log file: {FileName}", file.FileName);
                        }
                    }
                }

                await Task.CompletedTask;
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
                return 0;
            }
        }

        /// <summary>
        /// Reads the content of a log file
        /// </summary>
        public async Task<string> ReadLogFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return "Log file not found.";
                }

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log file: {FilePath}", filePath);
                return $"Error reading log file: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the last N lines from a log file
        /// </summary>
        public async Task<List<string>> GetLastLinesAsync(string filePath, int lineCount = 100)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new List<string> { "Log file not found." };
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                return lines.TakeLast(lineCount).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log file lines: {FilePath}", filePath);
                return new List<string> { $"Error reading log file: {ex.Message}" };
            }
        }

        /// <summary>
        /// Exports logs to a specified location
        /// </summary>
        public async Task<bool> ExportLogsAsync(string destinationPath)
        {
            try
            {
                var logFiles = GetLogFiles();
                var exportDir = Path.Combine(destinationPath, $"ExcelImportTool_Logs_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(exportDir);

                foreach (var logFile in logFiles)
                {
                    var destFile = Path.Combine(exportDir, logFile.FileName);
                    File.Copy(logFile.FilePath, destFile, true);
                }

                _logger.LogInformation("Exported {Count} log files to {Path}", logFiles.Count, exportDir);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting logs");
                return false;
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class LogFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string FormattedSize { get; set; } = string.Empty;
    }
}
