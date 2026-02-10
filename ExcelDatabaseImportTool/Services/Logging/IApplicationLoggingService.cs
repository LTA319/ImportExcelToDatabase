using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExcelDatabaseImportTool.Services.Logging
{
    /// <summary>
    /// Service for application-wide logging with diagnostic information
    /// </summary>
    public interface IApplicationLoggingService
    {
        /// <summary>
        /// Logs an information message with context
        /// </summary>
        void LogInformation(string message, string context, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs a warning message with context
        /// </summary>
        void LogWarning(string message, string context, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs an error with exception details
        /// </summary>
        void LogError(Exception exception, string message, string context, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs a critical error with exception details
        /// </summary>
        void LogCritical(Exception exception, string message, string context, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Logs import operation start
        /// </summary>
        void LogImportStart(string configurationName, string fileName, int recordCount);

        /// <summary>
        /// Logs import operation completion
        /// </summary>
        void LogImportComplete(string configurationName, string fileName, int successCount, int failureCount, TimeSpan duration);

        /// <summary>
        /// Logs import operation failure
        /// </summary>
        void LogImportFailure(string configurationName, string fileName, Exception exception, TimeSpan duration);

        /// <summary>
        /// Collects diagnostic information about the current system state
        /// </summary>
        Task<Dictionary<string, object>> CollectDiagnosticInfoAsync();

        /// <summary>
        /// Gets the current log file path
        /// </summary>
        string GetCurrentLogFilePath();
    }
}
