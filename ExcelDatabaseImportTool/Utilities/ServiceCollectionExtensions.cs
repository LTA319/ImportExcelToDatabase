using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // Database Context
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source=ExcelImportTool.db"));

            // Repositories
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IImportLogRepository, ImportLogRepository>();

            // Services
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
            services.AddScoped<IExcelReaderService, ExcelReaderService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<IValidationService, ValidationService>();
            services.AddScoped<IForeignKeyResolverService, ForeignKeyResolverService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<DatabaseConfigurationViewModel>();
            services.AddTransient<ImportConfigurationViewModel>();
            services.AddTransient<ImportExecutionViewModel>();

            return services;
        }
    }
}