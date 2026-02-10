using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Services.Excel;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Services.Navigation;
using ExcelDatabaseImportTool.Repositories;
using ExcelDatabaseImportTool.ViewModels;

namespace ExcelDatabaseImportTool.Utilities
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Configure Logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.AddDebug();
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
    }
}