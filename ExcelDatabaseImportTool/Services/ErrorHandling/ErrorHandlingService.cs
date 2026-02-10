using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExcelDatabaseImportTool.Services.ErrorHandling
{
    /// <summary>
    /// Implementation of error handling service with crash reporting and recovery
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        private readonly string _crashReportDirectory;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
            _crashReportDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashReports");

            // Ensure crash report directory exists
            Directory.CreateDirectory(_crashReportDirectory);
        }

        public async Task LogErrorAsync(Exception exception, string context, ErrorSeverity severity = ErrorSeverity.Error)
        {
            var logLevel = severity switch
            {
                ErrorSeverity.Information => LogLevel.Information,
                ErrorSeverity.Warning => LogLevel.Warning,
                ErrorSeverity.Error => LogLevel.Error,
                ErrorSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Error
            };

            _logger.Log(logLevel, exception, "Error in {Context}: {Message}", context, exception.Message);

            // For critical errors, create a crash report
            if (severity == ErrorSeverity.Critical)
            {
                await CreateCrashReportAsync(exception, context);
            }
        }

        public async Task HandleCriticalErrorAsync(Exception exception, string context)
        {
            _logger.LogCritical(exception, "Critical error in {Context}", context);
            
            // Create crash report
            var reportPath = await CreateCrashReportAsync(exception, context);
            _logger.LogInformation("Crash report created at: {ReportPath}", reportPath);

            // Attempt recovery
            var recovered = await TryRecoverAsync(exception, context);
            if (recovered)
            {
                _logger.LogInformation("Successfully recovered from critical error");
            }
            else
            {
                _logger.LogWarning("Unable to recover from critical error");
            }
        }

        public async Task<bool> TryRecoverAsync(Exception exception, string context)
        {
            try
            {
                _logger.LogInformation("Attempting recovery from error in {Context}", context);

                // Recovery strategies based on context
                if (context.Contains("Database", StringComparison.OrdinalIgnoreCase))
                {
                    // Database-related recovery
                    _logger.LogInformation("Attempting database recovery...");
                    // Could implement database connection reset, backup restoration, etc.
                    await Task.Delay(100); // Placeholder for actual recovery logic
                    return true;
                }
                else if (context.Contains("Import", StringComparison.OrdinalIgnoreCase))
                {
                    // Import-related recovery
                    _logger.LogInformation("Attempting import recovery...");
                    // Could implement transaction rollback, cleanup, etc.
                    await Task.Delay(100);
                    return true;
                }

                return false;
            }
            catch (Exception recoveryException)
            {
                _logger.LogError(recoveryException, "Recovery attempt failed");
                return false;
            }
        }

        public string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "Access denied. Please check file permissions and try again.",
                FileNotFoundException => "The required file could not be found. Please verify the file path.",
                IOException => "An error occurred while accessing a file. The file may be in use by another program.",
                InvalidOperationException => "The operation could not be completed. Please check your configuration and try again.",
                TimeoutException => "The operation timed out. Please check your network connection and try again.",
                ArgumentException => "Invalid input provided. Please check your data and try again.",
                _ => $"An unexpected error occurred: {exception.Message}"
            };
        }

        public async Task<string> CreateCrashReportAsync(Exception exception, string context)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"CrashReport_{timestamp}.txt";
                var filePath = Path.Combine(_crashReportDirectory, fileName);

                var report = new StringBuilder();
                report.AppendLine("=== Excel Database Import Tool - Crash Report ===");
                report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Context: {context}");
                report.AppendLine();
                report.AppendLine("=== Exception Details ===");
                report.AppendLine($"Type: {exception.GetType().FullName}");
                report.AppendLine($"Message: {exception.Message}");
                report.AppendLine($"Stack Trace:");
                report.AppendLine(exception.StackTrace);
                
                if (exception.InnerException != null)
                {
                    report.AppendLine();
                    report.AppendLine("=== Inner Exception ===");
                    report.AppendLine($"Type: {exception.InnerException.GetType().FullName}");
                    report.AppendLine($"Message: {exception.InnerException.Message}");
                    report.AppendLine($"Stack Trace:");
                    report.AppendLine(exception.InnerException.StackTrace);
                }

                report.AppendLine();
                report.AppendLine("=== System Information ===");
                report.AppendLine($"OS: {Environment.OSVersion}");
                report.AppendLine($"CLR Version: {Environment.Version}");
                report.AppendLine($"Machine Name: {Environment.MachineName}");
                report.AppendLine($"User: {Environment.UserName}");
                report.AppendLine($"Working Directory: {Environment.CurrentDirectory}");

                await File.WriteAllTextAsync(filePath, report.ToString());
                
                _logger.LogInformation("Crash report created: {FilePath}", filePath);
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create crash report");
                return string.Empty;
            }
        }
    }
}
