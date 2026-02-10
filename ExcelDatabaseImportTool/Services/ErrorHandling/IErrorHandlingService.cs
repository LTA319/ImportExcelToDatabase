using System;
using System.Threading.Tasks;

namespace ExcelDatabaseImportTool.Services.ErrorHandling
{
    /// <summary>
    /// Service for handling application errors and crashes
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Logs an error with context information
        /// </summary>
        Task LogErrorAsync(Exception exception, string context, ErrorSeverity severity = ErrorSeverity.Error);

        /// <summary>
        /// Handles a critical error with crash reporting
        /// </summary>
        Task HandleCriticalErrorAsync(Exception exception, string context);

        /// <summary>
        /// Attempts to recover from an error
        /// </summary>
        Task<bool> TryRecoverAsync(Exception exception, string context);

        /// <summary>
        /// Gets user-friendly error message
        /// </summary>
        string GetUserFriendlyMessage(Exception exception);

        /// <summary>
        /// Creates a crash report
        /// </summary>
        Task<string> CreateCrashReportAsync(Exception exception, string context);
    }

    public enum ErrorSeverity
    {
        Information,
        Warning,
        Error,
        Critical
    }
}
