# Design Document

## Overview

The Excel Database Import Tool is a .NET 8 WPF desktop application that provides a comprehensive solution for importing Excel data into MySQL and SQL Server databases. The application follows a layered architecture with clear separation between UI, business logic, data access, and storage layers. The system uses SQLite for local configuration and logging storage, implements robust error handling and transaction management, and provides an intuitive user interface for managing database connections, import configurations, and execution workflows.

## Architecture

The application follows a clean architecture pattern with the following layers:

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│         (WPF Views & ViewModels)        │
├─────────────────────────────────────────┤
│            Business Layer               │
│    (Services & Domain Logic)            │
├─────────────────────────────────────────┤
│           Data Access Layer             │
│  (Repositories & Database Adapters)     │
├─────────────────────────────────────────┤
│            Storage Layer                │
│   (SQLite, MySQL, SQL Server)          │
└─────────────────────────────────────────┘
```

**Key Architectural Principles:**
- **Dependency Inversion**: Higher-level modules don't depend on lower-level modules
- **Single Responsibility**: Each class has one reason to change
- **Interface Segregation**: Clients depend only on interfaces they use
- **Repository Pattern**: Abstracts data access logic
- **Unit of Work Pattern**: Manages transactions across multiple repositories

## Components and Interfaces

### Core Interfaces

```csharp
public interface IDatabaseConnectionService
{
    Task<bool> TestConnectionAsync(DatabaseConfiguration config);
    Task<IDbConnection> CreateConnectionAsync(DatabaseConfiguration config);
}

public interface IImportService
{
    Task<ImportResult> ImportDataAsync(ImportConfiguration config, string excelFilePath);
    event EventHandler<ImportProgressEventArgs> ProgressUpdated;
}

public interface IExcelReaderService
{
    Task<DataTable> ReadExcelFileAsync(string filePath);
    Task<List<string>> GetColumnNamesAsync(string filePath);
}

public interface IConfigurationRepository
{
    Task<List<DatabaseConfiguration>> GetDatabaseConfigurationsAsync();
    Task<List<ImportConfiguration>> GetImportConfigurationsAsync();
    Task SaveDatabaseConfigurationAsync(DatabaseConfiguration config);
    Task SaveImportConfigurationAsync(ImportConfiguration config);
}

public interface IImportLogRepository
{
    Task SaveImportLogAsync(ImportLog log);
    Task<List<ImportLog>> GetImportLogsAsync(DateTime? fromDate = null);
}
```

### Main Components

1. **MainWindow & ViewModels**
   - MainWindowViewModel: Orchestrates navigation between different views
   - DatabaseConfigurationViewModel: Manages database connection configurations
   - ImportConfigurationViewModel: Handles import mapping configurations
   - ImportExecutionViewModel: Controls the import execution process

2. **Services**
   - DatabaseConnectionService: Handles database connectivity for MySQL and SQL Server
   - ImportService: Core business logic for data import operations
   - ExcelReaderService: Reads and parses Excel files using EPPlus library
   - ValidationService: Validates data against database constraints
   - ForeignKeyResolverService: Resolves foreign key relationships

3. **Repositories**
   - ConfigurationRepository: Manages database and import configurations in SQLite
   - ImportLogRepository: Handles import operation logging
   - DatabaseRepository: Generic repository for target database operations

4. **Data Models**
   - DatabaseConfiguration: Connection settings and database type
   - ImportConfiguration: Field mappings and import rules
   - FieldMapping: Excel column to database field relationships
   - ForeignKeyMapping: Foreign key resolution configuration
   - ImportLog: Import operation records and error details

## Data Models

### DatabaseConfiguration
```csharp
public class DatabaseConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DatabaseType Type { get; set; } // MySQL, SqlServer
    public string Server { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string EncryptedPassword { get; set; }
    public int Port { get; set; }
    public string ConnectionString { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

### ImportConfiguration
```csharp
public class ImportConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int DatabaseConfigurationId { get; set; }
    public string TableName { get; set; }
    public List<FieldMapping> FieldMappings { get; set; }
    public bool HasHeaderRow { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
```

### FieldMapping
```csharp
public class FieldMapping
{
    public int Id { get; set; }
    public string ExcelColumnName { get; set; }
    public string DatabaseFieldName { get; set; }
    public bool IsRequired { get; set; }
    public string DataType { get; set; }
    public ForeignKeyMapping ForeignKeyMapping { get; set; }
}
```

### ForeignKeyMapping
```csharp
public class ForeignKeyMapping
{
    public int Id { get; set; }
    public string ReferencedTable { get; set; }
    public string ReferencedLookupField { get; set; }
    public string ReferencedKeyField { get; set; }
}
```

### ImportLog
```csharp
public class ImportLog
{
    public int Id { get; set; }
    public int ImportConfigurationId { get; set; }
    public string ExcelFileName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public ImportStatus Status { get; set; } // Success, Failed, Partial
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public string ErrorDetails { get; set; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After reviewing all properties identified in the prework, several can be consolidated to eliminate redundancy:

- Properties 4.1, 4.2, and 4.3 (logging behaviors) can be combined into a comprehensive logging property
- Properties 5.3, 5.4, and 5.5 (transaction behaviors) can be combined into a comprehensive transaction management property
- Properties 1.2 and 1.4 (configuration persistence) can be combined into a single persistence property

### Core Properties

**Property 1: Configuration persistence completeness**
*For any* database configuration with all required fields, storing it should result in all connection parameters (server, database, credentials, type) being retrievable from SQLite storage
**Validates: Requirements 1.2, 1.4**

**Property 2: Connection testing consistency**
*For any* database configuration, testing the connection should return a consistent result (success/failure with error details) when tested multiple times with the same parameters
**Validates: Requirements 1.3**

**Property 3: Referential integrity enforcement**
*For any* database configuration that is referenced by import configurations, attempting to delete it should be prevented and the configuration should remain in storage
**Validates: Requirements 1.5**

**Property 4: Import configuration validation**
*For any* import configuration, saving it should only succeed if all required database fields have corresponding Excel column mappings
**Validates: Requirements 2.2, 2.5**

**Property 5: Field mapping consistency**
*For any* valid field mapping between Excel columns and database fields, the mapping should preserve the null/not-null constraints and data type specifications
**Validates: Requirements 2.3**

**Property 6: Foreign key resolution accuracy**
*For any* configured foreign key mapping, resolving an Excel value should return the correct foreign key ID from the referenced table using the specified lookup field
**Validates: Requirements 2.4, 5.2**

**Property 7: File validation reliability**
*For any* file input, the validation process should correctly identify valid Excel files and reject invalid formats with appropriate error messages
**Validates: Requirements 3.2**

**Property 8: Error handling continuity**
*For any* import operation that encounters errors, the system should continue processing remaining records and collect all error details without stopping
**Validates: Requirements 3.4**

**Property 9: Import statistics accuracy**
*For any* completed import operation, the summary statistics should accurately reflect the total, successful, and failed record counts
**Validates: Requirements 3.5**

**Property 10: Comprehensive import logging**
*For any* import operation (successful, failed, or partial), a complete log entry should be created with timestamp, configuration details, status, record counts, and error information
**Validates: Requirements 4.1, 4.2, 4.3, 4.4**

**Property 11: Data validation integrity**
*For any* Excel data being processed, all required fields should be validated against not-null constraints before any database insertion occurs
**Validates: Requirements 5.1**

**Property 12: Transaction atomicity**
*For any* import operation, database changes should be committed only when all validations pass, or completely rolled back when any validation fails, with appropriate error logging
**Validates: Requirements 5.3, 5.4, 5.5**

**Property 13: Sensitive data encryption**
*For any* database configuration containing connection credentials, the sensitive information should be encrypted before storage and decryptable upon retrieval
**Validates: Requirements 6.2**

**Property 14: Complete log data storage**
*For any* import log entry, storing it should result in all required information (timestamps, operation details, error information) being persistently saved and retrievable
**Validates: Requirements 6.3**

## Error Handling

The application implements comprehensive error handling at multiple levels:

### Database Connection Errors
- Connection timeout handling with configurable retry logic
- Authentication failure detection and user-friendly error messages
- Network connectivity issues with offline mode capabilities
- Database-specific error code interpretation for MySQL and SQL Server

### Excel File Processing Errors
- File format validation with support for .xlsx and .xls formats
- Corrupted file detection and recovery suggestions
- Missing column handling with user notification
- Large file processing with memory management and progress tracking

### Data Validation Errors
- Required field validation with specific error messages
- Data type mismatch detection and conversion attempts
- Foreign key resolution failures with lookup table verification
- Constraint violation handling with rollback mechanisms

### Transaction Management
- Automatic rollback on any validation or insertion failure
- Deadlock detection and retry logic for concurrent operations
- Connection pooling management to prevent resource exhaustion
- Batch processing with configurable commit intervals for large datasets

### Logging and Recovery
- Structured error logging with severity levels and categorization
- Automatic backup creation before critical operations
- SQLite corruption detection with repair and recovery procedures
- Configuration export/import for disaster recovery scenarios

## Testing Strategy

The application will use a dual testing approach combining unit tests and property-based tests to ensure comprehensive coverage and correctness validation.

### Unit Testing Framework
- **Framework**: NUnit 3.x for .NET 8 compatibility
- **Mocking**: Moq for dependency isolation
- **Coverage**: Minimum 80% code coverage for business logic components
- **Focus Areas**:
  - Service layer business logic validation
  - Repository pattern implementation correctness
  - UI ViewModel behavior verification
  - Error handling path validation

### Property-Based Testing Framework
- **Framework**: FsCheck.NUnit for .NET property-based testing
- **Configuration**: Minimum 100 iterations per property test
- **Generator Strategy**: Custom generators for domain-specific data types (DatabaseConfiguration, ImportConfiguration, Excel data)
- **Property Test Requirements**:
  - Each property-based test must run 100+ iterations with random inputs
  - Each test must be tagged with format: **Feature: excel-database-import-tool, Property {number}: {property_text}**
  - Each correctness property must be implemented by exactly one property-based test
  - Tests must use realistic data generators that respect business constraints

### Testing Implementation Guidelines
- **Unit Tests**: Verify specific examples, edge cases, and integration points
- **Property Tests**: Verify universal properties across all valid inputs
- **Test Data**: Use realistic but anonymized sample data for Excel files and database schemas
- **Performance Tests**: Validate import performance with large datasets (10K+ records)
- **Integration Tests**: End-to-end workflow validation with real database connections

### Test Coverage Requirements
- All correctness properties must have corresponding property-based tests
- Critical error handling paths must have unit test coverage
- UI ViewModels must have behavior verification tests
- Database operations must have integration test coverage
- Configuration serialization/deserialization must have round-trip tests