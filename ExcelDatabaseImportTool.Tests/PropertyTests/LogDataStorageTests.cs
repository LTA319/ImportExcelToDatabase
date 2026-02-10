using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Repositories;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 14: Complete log data storage**
    /// **Validates: Requirements 6.3**
    /// </summary>
    public static class LogDataStorageTests
    {
        public static void RunLogDataStorageTests()
        {
            var results = new List<string>();
            results.Add("Running log data storage tests...");

            try
            {
                // Test various import log scenarios
                TestImportLogPersistence(results);
                
                results.Add("Log data storage tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during log data storage tests: {ex.Message}");
            }
            
            // Write results to file
            File.WriteAllLines("log_data_storage_test_results.txt", results);
        }

        private static void TestImportLogPersistence(List<string> results)
        {
            var testLogs = new[]
            {
                new ImportLog
                {
                    ImportConfigurationId = 1,
                    ExcelFileName = "employees.xlsx",
                    StartTime = DateTime.UtcNow.AddMinutes(-10),
                    EndTime = DateTime.UtcNow.AddMinutes(-5),
                    Status = ImportStatus.Success,
                    TotalRecords = 100,
                    SuccessfulRecords = 100,
                    FailedRecords = 0,
                    ErrorDetails = ""
                },
                new ImportLog
                {
                    ImportConfigurationId = 1,
                    ExcelFileName = "customers.xlsx",
                    StartTime = DateTime.UtcNow.AddMinutes(-20),
                    EndTime = DateTime.UtcNow.AddMinutes(-18),
                    Status = ImportStatus.Failed,
                    TotalRecords = 50,
                    SuccessfulRecords = 0,
                    FailedRecords = 50,
                    ErrorDetails = "Database connection failed: Timeout expired"
                },
                new ImportLog
                {
                    ImportConfigurationId = 1,
                    ExcelFileName = "products.xlsx",
                    StartTime = DateTime.UtcNow.AddMinutes(-30),
                    EndTime = DateTime.UtcNow.AddMinutes(-25),
                    Status = ImportStatus.Partial,
                    TotalRecords = 200,
                    SuccessfulRecords = 150,
                    FailedRecords = 50,
                    ErrorDetails = "Foreign key constraint violations on 50 records: Product category not found"
                },
                new ImportLog
                {
                    ImportConfigurationId = 1,
                    ExcelFileName = "large_dataset.xlsx",
                    StartTime = DateTime.UtcNow.AddHours(-1),
                    EndTime = null, // Still running
                    Status = ImportStatus.Failed,
                    TotalRecords = 10000,
                    SuccessfulRecords = 5000,
                    FailedRecords = 0,
                    ErrorDetails = "Process interrupted by user"
                }
            };

            foreach (var log in testLogs)
            {
                try
                {
                    // Create in-memory database for testing
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                        .Options;

                    using var context = new ApplicationDbContext(options);
                    var configRepository = new ConfigurationRepository(context);
                    var logRepository = new ImportLogRepository(context);

                    // First create required database and import configurations
                    var dbConfig = new DatabaseConfiguration
                    {
                        Name = "Test DB Config",
                        Type = DatabaseType.MySQL,
                        Server = "localhost",
                        Database = "testdb",
                        Username = "testuser",
                        EncryptedPassword = "encrypted_password",
                        Port = 3306,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                    configRepository.SaveDatabaseConfigurationAsync(dbConfig).Wait();

                    var importConfig = new ImportConfiguration
                    {
                        Name = "Test Import Config",
                        DatabaseConfigurationId = dbConfig.Id,
                        TableName = "TestTable",
                        HasHeaderRow = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                    configRepository.SaveImportConfigurationAsync(importConfig).Wait();

                    // Update log to reference the created import configuration
                    log.ImportConfigurationId = importConfig.Id;

                    // Act - Save the import log
                    logRepository.SaveImportLogAsync(log).Wait();

                    // Assert - Retrieve and verify all log information is persisted
                    var retrievedLog = logRepository.GetImportLogByIdAsync(log.Id).Result;

                    if (retrievedLog == null)
                    {
                        results.Add($"FAIL: Import log not found after save for: {log.ExcelFileName}");
                        continue;
                    }

                    // Verify all required information is persisted
                    var allLogDataPersisted = 
                        retrievedLog.ImportConfigurationId == log.ImportConfigurationId &&
                        retrievedLog.ExcelFileName == log.ExcelFileName &&
                        retrievedLog.StartTime.ToString("yyyy-MM-dd HH:mm:ss") == log.StartTime.ToString("yyyy-MM-dd HH:mm:ss") &&
                        retrievedLog.Status == log.Status &&
                        retrievedLog.TotalRecords == log.TotalRecords &&
                        retrievedLog.SuccessfulRecords == log.SuccessfulRecords &&
                        retrievedLog.FailedRecords == log.FailedRecords &&
                        retrievedLog.ErrorDetails == log.ErrorDetails;

                    // Handle nullable EndTime
                    var endTimeMatches = (retrievedLog.EndTime == null && log.EndTime == null) ||
                                       (retrievedLog.EndTime != null && log.EndTime != null && 
                                        retrievedLog.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss") == log.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

                    if (!allLogDataPersisted || !endTimeMatches)
                    {
                        results.Add($"FAIL: Not all log data persisted correctly for: {log.ExcelFileName}");
                        results.Add($"  Expected: {log.ExcelFileName}, Status: {log.Status}, Total: {log.TotalRecords}, Success: {log.SuccessfulRecords}, Failed: {log.FailedRecords}");
                        results.Add($"  Actual: {retrievedLog.ExcelFileName}, Status: {retrievedLog.Status}, Total: {retrievedLog.TotalRecords}, Success: {retrievedLog.SuccessfulRecords}, Failed: {retrievedLog.FailedRecords}");
                        if (!endTimeMatches)
                        {
                            results.Add($"  EndTime mismatch - Expected: {log.EndTime}, Actual: {retrievedLog.EndTime}");
                        }
                        continue;
                    }

                    // Verify timestamps are preserved
                    var timestampsPreserved = 
                        Math.Abs((retrievedLog.StartTime - log.StartTime).TotalSeconds) < 1;

                    if (!timestampsPreserved)
                    {
                        results.Add($"FAIL: Timestamps not preserved correctly for: {log.ExcelFileName}");
                        continue;
                    }

                    // Verify error information is stored
                    var errorInfoStored = retrievedLog.ErrorDetails == log.ErrorDetails;

                    if (!errorInfoStored)
                    {
                        results.Add($"FAIL: Error information not stored correctly for: {log.ExcelFileName}");
                        results.Add($"  Expected error: {log.ErrorDetails}");
                        results.Add($"  Actual error: {retrievedLog.ErrorDetails}");
                        continue;
                    }

                    results.Add($"PASS: All log data stored correctly for: {log.ExcelFileName} (Status: {log.Status})");
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception during log storage test for '{log.ExcelFileName}': {ex.Message}");
                }
            }

            // Test log retrieval by date range
            TestLogRetrievalByDateRange(results);
        }

        private static void TestLogRetrievalByDateRange(List<string> results)
        {
            try
            {
                // Create in-memory database for testing
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new ApplicationDbContext(options);
                var configRepository = new ConfigurationRepository(context);
                var logRepository = new ImportLogRepository(context);

                // Create required configurations
                var dbConfig = new DatabaseConfiguration
                {
                    Name = "Test DB Config",
                    Type = DatabaseType.MySQL,
                    Server = "localhost",
                    Database = "testdb",
                    Username = "testuser",
                    EncryptedPassword = "encrypted_password",
                    Port = 3306,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                configRepository.SaveDatabaseConfigurationAsync(dbConfig).Wait();

                var importConfig = new ImportConfiguration
                {
                    Name = "Test Import Config",
                    DatabaseConfigurationId = dbConfig.Id,
                    TableName = "TestTable",
                    HasHeaderRow = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                configRepository.SaveImportConfigurationAsync(importConfig).Wait();

                // Create logs with different dates
                var now = DateTime.UtcNow;
                var logs = new[]
                {
                    new ImportLog
                    {
                        ImportConfigurationId = importConfig.Id,
                        ExcelFileName = "old_file.xlsx",
                        StartTime = now.AddDays(-10),
                        EndTime = now.AddDays(-10).AddMinutes(5),
                        Status = ImportStatus.Success,
                        TotalRecords = 10,
                        SuccessfulRecords = 10,
                        FailedRecords = 0,
                        ErrorDetails = ""
                    },
                    new ImportLog
                    {
                        ImportConfigurationId = importConfig.Id,
                        ExcelFileName = "recent_file.xlsx",
                        StartTime = now.AddDays(-1),
                        EndTime = now.AddDays(-1).AddMinutes(5),
                        Status = ImportStatus.Success,
                        TotalRecords = 20,
                        SuccessfulRecords = 20,
                        FailedRecords = 0,
                        ErrorDetails = ""
                    }
                };

                // Save all logs
                foreach (var log in logs)
                {
                    logRepository.SaveImportLogAsync(log).Wait();
                }

                // Test retrieval by date range
                var recentLogs = logRepository.GetImportLogsAsync(now.AddDays(-2), now).Result;
                
                if (recentLogs.Count != 1 || recentLogs[0].ExcelFileName != "recent_file.xlsx")
                {
                    results.Add($"FAIL: Date range filtering not working correctly");
                    results.Add($"  Expected 1 recent log, got {recentLogs.Count}");
                    return;
                }

                results.Add("PASS: Log retrieval by date range works correctly");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during date range test: {ex.Message}");
            }
        }
    }
}