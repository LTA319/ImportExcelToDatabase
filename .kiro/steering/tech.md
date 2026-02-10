# Technology Stack

## Framework & Runtime

- **.NET 8.0** (net8.0-windows)
- **WPF** (Windows Presentation Foundation) for UI
- **C#** with nullable reference types enabled
- Implicit usings enabled

## Key Libraries & Packages

### Main Application
- **EPPlus 8.4.2** - Excel file reading/writing (NonCommercial license)
- **Entity Framework Core 8.0.11** - ORM and SQLite provider
- **Microsoft.Data.SqlClient 6.1.4** - SQL Server connectivity
- **MySql.Data 9.6.0** - MySQL connectivity
- **Serilog** - Structured logging with file rotation
  - Serilog.Extensions.Hosting
  - Serilog.Sinks.Console
  - Serilog.Sinks.File
  - Serilog.Enrichers.Environment
  - Serilog.Enrichers.Thread
- **Microsoft.Extensions.Hosting 8.0.1** - Dependency injection and hosting

### Testing
- **NUnit 4.4.0** - Test framework
- **FsCheck.NUnit 3.3.2** - Property-based testing
- **Moq 4.20.72** - Mocking framework
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for tests
- **coverlet.collector** - Code coverage

## Build & Run Commands

### Build Solution
```bash
cd ExcelDatabaseImportTool
dotnet build ExcelDatabaseImportTool.sln
```

### Run Application
```bash
cd ExcelDatabaseImportTool
dotnet run
```

Or run the compiled executable:
```bash
ExcelDatabaseImportTool\bin\Debug\net8.0-windows\ExcelDatabaseImportTool.exe
```

### Run Tests
```bash
# From test project directory
cd ExcelDatabaseImportTool.Tests
dotnet test

# From solution directory
cd ExcelDatabaseImportTool
dotnet test ExcelDatabaseImportTool.sln
```

### Clean Build
```bash
dotnet clean
dotnet build
```

## Database

- **SQLite** for local application database (ExcelImportTool.db)
- **Entity Framework Core** with code-first migrations
- Connection pooling and query tracking enabled
- Sensitive data logging in DEBUG mode only

## Logging

- Logs stored in `Logs/` directory in application base directory
- Daily rolling log files with 30-day retention
- 10 MB file size limit with automatic rollover
- Structured logging format with timestamp, level, context, and thread ID
