using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Services.Excel;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Services.Navigation;
using ExcelDatabaseImportTool.Services.ErrorHandling;
using ExcelDatabaseImportTool.Services.Logging;
using ExcelDatabaseImportTool.Repositories;
using ExcelDatabaseImportTool.ViewModels;

namespace ExcelDatabaseImportTool.Utilities
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Configure Serilog for structured logging with file output
            ConfigureSerilog();

            // Configure Logging with Serilog
            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddSerilog(dispose: true);
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // Database Context with connection pooling and error handling
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                var connectionString = "Data Source=ExcelImportTool.db";
                options.UseSqlite(connectionString);
                
                // Enable sensitive data logging in debug mode
                #if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                #endif
                
                // Configure command timeout
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            }, ServiceLifetime.Scoped);

            // Database Initialization Service
            services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

            // Repositories
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IImportLogRepository, ImportLogRepository>();

            // Core Services
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
            services.AddScoped<IExcelReaderService, ExcelReaderService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IForeignKeyResolverService, ForeignKeyResolverService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            
            // Error Handling Service
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            
            // Application Logging Service
            services.AddSingleton<IApplicationLoggingService, ApplicationLoggingService>();
            
            // Navigation Service (Singleton for application-wide state)
            services.AddSingleton<INavigationService, NavigationService>();

            // ViewModels (Transient for fresh instances per view)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<DatabaseConfigurationViewModel>();
            services.AddTransient<ImportConfigurationViewModel>();
            services.AddTransient<ImportExecutionViewModel>();
            services.AddTransient<ImportHistoryViewModel>();

            return services;
        }

        private static void ConfigureSerilog()
        {
            // Get log directory path
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ExcelDatabaseImportTool",
                "Logs");

            // Ensure log directory exists
            Directory.CreateDirectory(logDirectory);

            // Configure Serilog with file output and rotation
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithThreadId()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "application-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB per file
                    rollOnFileSizeLimit: true)
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Serilog configured successfully. Logs will be written to: {LogDirectory}", logDirectory);
        }
    }
}