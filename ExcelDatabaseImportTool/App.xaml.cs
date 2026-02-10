using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Utilities;
using ExcelDatabaseImportTool.ViewModels;
using OfficeOpenXml;

namespace ExcelDatabaseImportTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Set EPPlus license first - try different approaches for EPPlus 8.x
        try
        {
            // This should work for EPPlus 8.x but the API might be different
            var licenseType = typeof(ExcelPackage).Assembly.GetType("OfficeOpenXml.LicenseType");
            if (licenseType != null)
            {
                var nonCommercialValue = Enum.Parse(licenseType, "NonCommercial");
                var setLicenseMethod = typeof(ExcelPackage).GetProperty("License")?.PropertyType.GetMethod("SetLicense");
                if (setLicenseMethod != null)
                {
                    setLicenseMethod.Invoke(ExcelPackage.License, new[] { nonCommercialValue });
                }
            }
        }
        catch
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            }
            catch
            {
                // License setting failed, continue anyway
            }
        }

        // Call base first to ensure proper WPF initialization
        base.OnStartup(e);

        // Build the host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddApplicationServices();
            })
            .Build();

        // Initialize database
        await InitializeDatabaseAsync();

        // Run file validation tests
        TestRunner.RunFileValidationTests();

        // Run data validation tests
        TestRunner.RunDataValidationTests();

        // Run foreign key resolution tests
        TestRunner.RunForeignKeyResolutionTests();

        // Start the host
        await _host.StartAsync();

        // Create and show main window
        var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow
        {
            DataContext = mainWindowViewModel
        };
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Check if database exists and has the correct schema
            var canConnect = await context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                // Database doesn't exist, create it
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                // Database exists, check if we need to apply migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    // There are pending migrations, but since we're using EnsureCreated,
                    // we need to delete and recreate for now
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize database: {ex.Message}", "Database Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}

