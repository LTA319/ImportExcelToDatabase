# Project Structure

## Solution Organization

The solution contains two projects:

- **ExcelDatabaseImportTool/** - Main WPF application
- **ExcelDatabaseImportTool.Tests/** - Test project (unit, integration, property-based tests)

## Main Application Structure

```
ExcelDatabaseImportTool/
├── Commands/              # ICommand implementations (RelayCommand, AsyncRelayCommand)
├── Converters/            # WPF value converters for data binding
├── Data/
│   └── Context/          # Entity Framework DbContext
│   └── Migrations/       # EF Core migrations
├── Interfaces/
│   ├── Repositories/     # Repository interfaces
│   └── Services/         # Service interfaces
├── Models/
│   ├── Configuration/    # Configuration models (DatabaseConfiguration, ImportConfiguration, FieldMapping, ForeignKeyMapping)
│   └── Domain/          # Domain models (ImportLog, ImportStatus, DatabaseType)
├── Repositories/         # Repository implementations
├── Services/
│   ├── Database/        # Database connection, initialization, encryption
│   ├── ErrorHandling/   # Error handling and recovery
│   ├── Excel/           # Excel file reading
│   ├── Import/          # Import execution, validation, foreign key resolution
│   ├── Logging/         # Application logging and log file management
│   └── Navigation/      # View navigation service
├── Utilities/            # Extension methods (ServiceCollectionExtensions)
├── ViewModels/           # MVVM ViewModels (inherit from BaseViewModel)
├── Views/                # WPF Views (.xaml and .xaml.cs)
├── App.xaml[.cs]         # Application entry point
└── MainWindow.xaml[.cs]  # Main window shell
```

## Test Project Structure

```
ExcelDatabaseImportTool.Tests/
├── IntegrationTests/     # End-to-end integration tests
├── PerformanceTests/     # Large dataset performance tests
├── PropertyTests/        # FsCheck property-based tests
├── UnitTests/            # Traditional unit tests
└── GlobalSetup.cs        # Test initialization (EPPlus license)
```

## Architecture Patterns

### MVVM (Model-View-ViewModel)
- **Views**: XAML files in `Views/` folder
- **ViewModels**: Inherit from `BaseViewModel` with `INotifyPropertyChanged`
- **Models**: Domain and configuration models in `Models/`
- **Commands**: `RelayCommand` and `AsyncRelayCommand` for user actions

### Dependency Injection
- Configured in `ServiceCollectionExtensions.AddApplicationServices()`
- Service lifetimes:
  - **Scoped**: DbContext, repositories, most services
  - **Singleton**: Navigation, error handling, logging services
  - **Transient**: ViewModels (fresh instance per view)

### Repository Pattern
- Interfaces in `Interfaces/Repositories/`
- Implementations in `Repositories/`
- Abstracts data access from business logic

### Service Layer
- Organized by domain concern (Database, Excel, Import, etc.)
- Interface-based for testability
- Injected via constructor dependency injection

## Key Conventions

### Naming
- ViewModels end with `ViewModel`
- Services end with `Service`
- Repositories end with `Repository`
- Interfaces prefixed with `I`

### File Organization
- One class per file
- File name matches class name
- Related classes grouped in folders by domain

### Code Style
- Nullable reference types enabled
- XML documentation comments on public APIs
- Async methods suffixed with `Async`
- CallerMemberName attribute for property change notifications

## Configuration & Data Files

- **ExcelImportTool.db** - SQLite database (root directory)
- **Logs/** - Application log files (created in app base directory)
- **Backups/** - Backup storage (currently empty)

## Important Notes

- Main project has `IsTestProject=false` to keep it clean
- All test code isolated in separate test project
- EPPlus license must be set at application startup
- Database auto-initializes on first run with migrations
