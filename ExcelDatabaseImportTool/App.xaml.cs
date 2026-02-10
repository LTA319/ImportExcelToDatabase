using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Services.Database;
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
    private ILogger<App>? _logger;

    public App()
    {
        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Set EPPlus license first - try different approaches for EPPlus 8.x
        ConfigureEPPlusLicense();

        // Call base first to ensure proper WPF initialization
        base.OnStartup(e);

        try
        {
            // Build the host with dependency injection
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddApplicationServices();
                })
                .Build();

            // Get logger after host is built
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Application starting...");

            // Start the host
            await _host.StartAsync();
            _logger.LogInformation("Host started successfully");

            // Initialize database
            await InitializeDatabaseAsync();
            _logger.LogInformation("Database initialized successfully");

            // Create and show main window
            var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            
            _logger.LogInformation("Main window created, showing application");
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Fatal error during application startup");
            
            MessageBox.Show(
                $"A critical error occurred during application startup:\n\n{ex.Message}\n\nThe application will now close.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Application shutting down...");
            
            if (_host != null)
            {
                // Ensure all pending database operations are completed
                using (var scope = _host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await context.SaveChangesAsync();
                }
                
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
                _logger?.LogInformation("Host stopped and disposed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during application shutdown");
        }
        finally
        {
            base.OnExit(e);
        }
    }

    private void ConfigureEPPlusLicense()
    {
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
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            using var scope = _host!.Services.CreateScope();
            var initService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
            
            _logger?.LogInformation("Checking database status...");
            
            // Check if database exists
            var dbExists = await initService.DatabaseExistsAsync();
            
            if (!dbExists)
            {
                _logger?.LogInformation("Database does not exist, creating new database...");
                await initService.InitializeDatabaseAsync();
                _logger?.LogInformation("Database created successfully");
            }
            else
            {
                _logger?.LogInformation("Database exists, checking for migrations...");
                await initService.MigrateDatabaseAsync();
                _logger?.LogInformation("Database migrations applied successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize database");
            
            MessageBox.Show(
                $"Failed to initialize database:\n\n{ex.Message}\n\nPlease ensure the application has write permissions to the application directory.",
                "Database Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            throw;
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger?.LogCritical(exception, "Unhandled exception in AppDomain");
        
        MessageBox.Show(
            $"A critical unhandled error occurred:\n\n{exception?.Message}\n\nThe application will now close.",
            "Critical Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled exception in Dispatcher");
        
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease try again or restart the application.",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        // Mark as handled to prevent application crash
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved task exception");
        
        // Mark as observed to prevent application crash
        e.SetObserved();
    }
}

