using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExcelDatabaseImportTool.Services.Logging
{
    /// <summary>
    /// Implementation of application logging service with diagnostic information collection
    /// </summary>
    public class ApplicationLoggingService : IApplicationLoggingService
    {
        private readonly ILogger<ApplicationLoggingService> _logger;
        private readonly string _logDirectory;

        public ApplicationLoggingService(ILogger<ApplicationLoggingService> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ExcelDatabaseImportTool",
                "Logs");
        }

        public void LogInformation(string message, string context, Dictionary<string, object>? properties = null)
        {
            using var scope = CreateLogScope(context, properties);
            _logger.LogInformation("{Message}", message);
        }

        public void LogWarning(string message, string context, Dictionary<string, object>? properties = null)
        {
            using var scope = CreateLogScope(context, properties);
            _logger.LogWarning("{Message}", message);
        }

        public void LogError(Exception exception, string message, string context, Dictionary<string, object>? properties = null)
        {
            using var scope = CreateLogScope(context, properties);
            _logger.LogError(exception, "{Message}", message);
        }

        public void LogCritical(Exception exception, string message, string context, Dictionary<string, object>? properties = null)
        {
            using var scope = CreateLogScope(context, properties);
            _logger.LogCritical(exception, "{Message}", message);
        }

        public void LogImportStart(string configurationName, string fileName, int recordCount)
        {
            var properties = new Dictionary<string, object>
            {
                ["ConfigurationName"] = configurationName,
                ["FileName"] = fileName,
                ["RecordCount"] = recordCount
            };

            using var scope = CreateLogScope("Import", properties);
            _logger.LogInformation(
                "Import started: Configuration={ConfigurationName}, File={FileName}, Records={RecordCount}",
                configurationName, fileName, recordCount);
        }

        public void LogImportComplete(string configurationName, string fileName, int successCount, int failureCount, TimeSpan duration)
        {
            var properties = new Dictionary<string, object>
            {
                ["ConfigurationName"] = configurationName,
                ["FileName"] = fileName,
                ["SuccessCount"] = successCount,
                ["FailureCount"] = failureCount,
                ["DurationMs"] = duration.TotalMilliseconds
            };

            using var scope = CreateLogScope("Import", properties);
            _logger.LogInformation(
                "Import completed: Configuration={ConfigurationName}, File={FileName}, Success={SuccessCount}, Failed={FailureCount}, Duration={Duration}ms",
                configurationName, fileName, successCount, failureCount, duration.TotalMilliseconds);
        }

        public void LogImportFailure(string configurationName, string fileName, Exception exception, TimeSpan duration)
        {
            var properties = new Dictionary<string, object>
            {
                ["ConfigurationName"] = configurationName,
                ["FileName"] = fileName,
                ["DurationMs"] = duration.TotalMilliseconds
            };

            using var scope = CreateLogScope("Import", properties);
            _logger.LogError(exception,
                "Import failed: Configuration={ConfigurationName}, File={FileName}, Duration={Duration}ms",
                configurationName, fileName, duration.TotalMilliseconds);
        }

        public async Task<Dictionary<string, object>> CollectDiagnosticInfoAsync()
        {
            var diagnostics = new Dictionary<string, object>();

            try
            {
                // System information
                diagnostics["MachineName"] = Environment.MachineName;
                diagnostics["OSVersion"] = Environment.OSVersion.ToString();
                diagnostics["CLRVersion"] = Environment.Version.ToString();
                diagnostics["ProcessorCount"] = Environment.ProcessorCount;
                diagnostics["Is64BitOS"] = Environment.Is64BitOperatingSystem;
                diagnostics["Is64BitProcess"] = Environment.Is64BitProcess;
                diagnostics["UserName"] = Environment.UserName;
                diagnostics["UserDomainName"] = Environment.UserDomainName;
                diagnostics["CurrentDirectory"] = Environment.CurrentDirectory;

                // Process information
                var currentProcess = Process.GetCurrentProcess();
                diagnostics["ProcessId"] = currentProcess.Id;
                diagnostics["ProcessName"] = currentProcess.ProcessName;
                diagnostics["WorkingSet"] = FormatBytes(currentProcess.WorkingSet64);
                diagnostics["PrivateMemory"] = FormatBytes(currentProcess.PrivateMemorySize64);
                diagnostics["VirtualMemory"] = FormatBytes(currentProcess.VirtualMemorySize64);
                diagnostics["ThreadCount"] = currentProcess.Threads.Count;
                diagnostics["HandleCount"] = currentProcess.HandleCount;

                // Memory information
                var gcMemory = GC.GetTotalMemory(false);
                diagnostics["GCMemory"] = FormatBytes(gcMemory);
                diagnostics["GCGen0Collections"] = GC.CollectionCount(0);
                diagnostics["GCGen1Collections"] = GC.CollectionCount(1);
                diagnostics["GCGen2Collections"] = GC.CollectionCount(2);

                // Disk information
                var logDrive = new DriveInfo(Path.GetPathRoot(_logDirectory) ?? "C:\\");
                if (logDrive.IsReady)
                {
                    diagnostics["LogDriveTotalSpace"] = FormatBytes(logDrive.TotalSize);
                    diagnostics["LogDriveFreeSpace"] = FormatBytes(logDrive.AvailableFreeSpace);
                    diagnostics["LogDriveUsedSpace"] = FormatBytes(logDrive.TotalSize - logDrive.AvailableFreeSpace);
                }

                // Log file information
                if (Directory.Exists(_logDirectory))
                {
                    var logFiles = Directory.GetFiles(_logDirectory, "*.log");
                    diagnostics["LogFileCount"] = logFiles.Length;
                    
                    var totalLogSize = logFiles.Sum(f => new FileInfo(f).Length);
                    diagnostics["TotalLogSize"] = FormatBytes(totalLogSize);
                }

                // Application uptime
                diagnostics["ApplicationUptime"] = (DateTime.Now - currentProcess.StartTime).ToString(@"dd\.hh\:mm\:ss");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting diagnostic information");
                diagnostics["Error"] = ex.Message;
            }

            return diagnostics;
        }

        public string GetCurrentLogFilePath()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(_logDirectory, $"application-{today}.log");
        }

        private IDisposable? CreateLogScope(string context, Dictionary<string, object>? properties)
        {
            var scopeData = new Dictionary<string, object>
            {
                ["Context"] = context
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    scopeData[kvp.Key] = kvp.Value;
                }
            }

            return _logger.BeginScope(scopeData);
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
}
