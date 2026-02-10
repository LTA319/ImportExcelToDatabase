using NUnit.Framework;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Services.Excel;
using ExcelDatabaseImportTool.Services.Database;
using ExcelDatabaseImportTool.Repositories;
using ExcelDatabaseImportTool.Data.Context;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using OfficeOpenXml;

namespace ExcelDatabaseImportTool.Tests.PerformanceTests
{
    /// <summary>
    /// Performance tests for large dataset processing
    /// Tests memory usage, processing time, and concurrent operations
    /// Requirements: 3.4, 5.3
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class LargeDatasetTests
    {
        private ApplicationDbContext _context = null!;
        private ConfigurationRepository _configRepository = null!;
        private ImportLogRepository _logRepository = null!;
        private ExcelReaderService _excelService = null!;
        private ValidationService _validationService = null!;
        private ForeignKeyResolverService _foreignKeyService = null!;
        private DatabaseConnectionService _connectionService = null!;
        private EncryptionService _encryptionService = null!;
        private ImportService _importService = null!;
        private string _testFilesDirectory = null!;

        [SetUp]
        public void Setup()
        {
            // Set EPPlus license for version 8+ using reflection to avoid version-specific dependencies
            try
            {
                var licenseProperty = typeof(ExcelPackage).GetProperty("License", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (licenseProperty != null)
                {
                    var licenseObject = licenseProperty.GetValue(null);
                    if (licenseObject != null)
                    {
                        var contextProperty = licenseObject.GetType().GetProperty("Context");
                        if (contextProperty != null)
                        {
                            var licenseContextType = typeof(ExcelPackage).Assembly.GetType("OfficeOpenXml.LicenseContext");
                            if (licenseContextType != null)
                            {
                                var nonCommercialValue = Enum.Parse(licenseContextType, "NonCommercial");
                                contextProperty.SetValue(licenseObject, nonCommercialValue);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, try the old API
                try
                {
#pragma warning disable CS0618
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618
                }
                catch
                {
                    // License setting failed, continue anyway
                }
            }

            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"PerfTestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _configRepository = new ConfigurationRepository(_context);
            _logRepository = new ImportLogRepository(_context);
            _excelService = new ExcelReaderService();
            _encryptionService = new EncryptionService();
            _connectionService = new DatabaseConnectionService(_encryptionService);
            _validationService = new ValidationService();
            _foreignKeyService = new ForeignKeyResolverService(_connectionService);
            
            _importService = new ImportService(
                _excelService,
                _validationService,
                _foreignKeyService,
                _connectionService,
                _logRepository,
                _configRepository
            );

            // Create test files directory
            _testFilesDirectory = Path.Combine(Path.GetTempPath(), "ExcelImportTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testFilesDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();

            // Clean up test files
            if (Directory.Exists(_testFilesDirectory))
            {
                try
                {
                    Directory.Delete(_testFilesDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Test]
        [Timeout(30000)] // 30 seconds max
        public async Task ImportService_ProcessLargeDataset_CompletesWithinTimeLimit()
        {
            // Arrange
            var testData = GenerateLargeTestDataset(10000); // 10K records
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var processedCount = 0;
            foreach (var record in testData)
            {
                // Simulate processing
                await Task.Run(async () =>
                {
                    var validated = await _validationService.ValidateDataRowAsync(
                        CreateDataRow(record),
                        CreateTestFieldMappings()
                    );
                    if (validated.IsValid)
                    {
                        processedCount++;
                    }
                });
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(processedCount, Is.EqualTo(10000));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30000), 
                $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
            
            // Log performance metrics
            TestContext.WriteLine($"Processed {processedCount} records in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / processedCount:F2}ms per record");
        }

        [Test]
        [Timeout(60000)] // 60 seconds max
        public async Task ExcelReader_ReadLargeFile_CompletesWithinTimeLimit()
        {
            // Arrange - Create a large Excel file with 10K+ records
            var filePath = Path.Combine(_testFilesDirectory, "large_dataset_10k.xlsx");
            CreateLargeExcelFile(filePath, 10000);
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var dataTable = await _excelService.ReadExcelFileAsync(filePath);
            stopwatch.Stop();
            
            // Assert
            Assert.That(dataTable.Rows.Count, Is.EqualTo(10000));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(60000), 
                $"Reading took {stopwatch.ElapsedMilliseconds}ms, expected < 60000ms");
            
            TestContext.WriteLine($"Read {dataTable.Rows.Count} records in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / dataTable.Rows.Count:F2}ms per record");
        }

        [Test]
        [Timeout(90000)] // 90 seconds max
        public async Task ExcelReader_ReadVeryLargeFile_CompletesWithinTimeLimit()
        {
            // Arrange - Create a very large Excel file with 50K records
            var filePath = Path.Combine(_testFilesDirectory, "large_dataset_50k.xlsx");
            CreateLargeExcelFile(filePath, 50000);
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var dataTable = await _excelService.ReadExcelFileAsync(filePath);
            stopwatch.Stop();
            
            // Assert
            Assert.That(dataTable.Rows.Count, Is.EqualTo(50000));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(90000), 
                $"Reading took {stopwatch.ElapsedMilliseconds}ms, expected < 90000ms");
            
            TestContext.WriteLine($"Read {dataTable.Rows.Count} records in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / dataTable.Rows.Count:F2}ms per record");
        }

        [Test]
        public async Task ImportService_MemoryUsage_StaysWithinLimits()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var testData = GenerateLargeTestDataset(5000); // 5K records
            
            // Act
            foreach (var record in testData)
            {
                await Task.Run(async () =>
                {
                    var validated = await _validationService.ValidateDataRowAsync(
                        CreateDataRow(record),
                        CreateTestFieldMappings()
                    );
                });
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = (finalMemory - initialMemory) / 1024 / 1024; // Convert to MB
            
            // Assert
            Assert.That(memoryIncrease, Is.LessThan(100), 
                $"Memory increased by {memoryIncrease}MB, expected < 100MB");
            
            TestContext.WriteLine($"Memory increase: {memoryIncrease}MB");
        }

        [Test]
        public async Task ExcelReader_MemoryUsageWithLargeFile_StaysWithinLimits()
        {
            // Arrange
            var filePath = Path.Combine(_testFilesDirectory, "memory_test_20k.xlsx");
            CreateLargeExcelFile(filePath, 20000);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act
            var dataTable = await _excelService.ReadExcelFileAsync(filePath);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            
            var memoryIncrease = (finalMemory - initialMemory) / 1024 / 1024; // Convert to MB
            
            // Assert
            Assert.That(dataTable.Rows.Count, Is.EqualTo(20000));
            Assert.That(memoryIncrease, Is.LessThan(200), 
                $"Memory increased by {memoryIncrease}MB for 20K records, expected < 200MB");
            
            TestContext.WriteLine($"Memory increase for 20K records: {memoryIncrease}MB");
            TestContext.WriteLine($"Average: {(double)memoryIncrease / 20:F2}MB per 1K records");
        }

        [Test]
        public async Task ValidationService_MemoryUsageWithLargeDataset_StaysWithinLimits()
        {
            // Arrange
            var testData = GenerateLargeTestDataset(15000);
            var fieldMappings = CreateTestFieldMappings();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act
            var validCount = 0;
            foreach (var record in testData)
            {
                var result = await _validationService.ValidateDataRowAsync(
                    CreateDataRow(record),
                    fieldMappings
                );
                
                if (result.IsValid)
                {
                    validCount++;
                }
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            
            var memoryIncrease = (finalMemory - initialMemory) / 1024 / 1024; // Convert to MB
            
            // Assert
            Assert.That(validCount, Is.GreaterThan(0));
            Assert.That(memoryIncrease, Is.LessThan(150), 
                $"Memory increased by {memoryIncrease}MB for 15K validations, expected < 150MB");
            
            TestContext.WriteLine($"Validated {validCount} records");
            TestContext.WriteLine($"Memory increase: {memoryIncrease}MB");
        }

        [Test]
        [Timeout(60000)] // 60 seconds max
        public async Task ImportService_ConcurrentImports_HandlesMultipleOperations()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<int>>();
            
            // Act - Simulate 3 concurrent import operations
            for (int i = 0; i < 3; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var testData = GenerateLargeTestDataset(1000);
                    var processedCount = 0;
                    
                    foreach (var record in testData)
                    {
                        var validated = await _validationService.ValidateDataRowAsync(
                            CreateDataRow(record),
                            CreateTestFieldMappings()
                        );
                        
                        if (validated.IsValid)
                        {
                            processedCount++;
                        }
                    }
                    
                    return processedCount;
                }));
            }
            
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            Assert.That(results.Sum(), Is.EqualTo(3000)); // 3 x 1000 records
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(60000));
            
            TestContext.WriteLine($"Processed {results.Sum()} records concurrently in {stopwatch.ElapsedMilliseconds}ms");
        }

        [Test]
        [Timeout(120000)] // 120 seconds max
        public async Task ExcelReader_ConcurrentFileReads_HandlesMultipleOperations()
        {
            // Arrange - Create multiple Excel files
            var files = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(_testFilesDirectory, $"concurrent_test_{i}.xlsx");
                CreateLargeExcelFile(filePath, 2000);
                files.Add(filePath);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Read files concurrently
            var tasks = files.Select(async file =>
            {
                var dataTable = await _excelService.ReadExcelFileAsync(file);
                return dataTable.Rows.Count;
            }).ToList();
            
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            Assert.That(results.Sum(), Is.EqualTo(10000)); // 5 x 2000 records
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(120000));
            
            TestContext.WriteLine($"Read {results.Sum()} records from {files.Count} files concurrently in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average per file: {stopwatch.ElapsedMilliseconds / files.Count}ms");
        }

        [Test]
        [Timeout(90000)] // 90 seconds max
        public async Task ValidationService_ConcurrentValidations_HandlesMultipleOperations()
        {
            // Arrange
            var datasets = new List<List<TestRecord>>();
            for (int i = 0; i < 4; i++)
            {
                datasets.Add(GenerateLargeTestDataset(2500));
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act - Validate datasets concurrently
            var tasks = datasets.Select(async dataset =>
            {
                var validCount = 0;
                var fieldMappings = CreateTestFieldMappings();
                
                foreach (var record in dataset)
                {
                    var result = await _validationService.ValidateDataRowAsync(
                        CreateDataRow(record),
                        fieldMappings
                    );
                    
                    if (result.IsValid)
                    {
                        validCount++;
                    }
                }
                
                return validCount;
            }).ToList();
            
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            Assert.That(results.Sum(), Is.EqualTo(10000)); // 4 x 2500 records
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(90000));
            
            TestContext.WriteLine($"Validated {results.Sum()} records concurrently in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average per dataset: {stopwatch.ElapsedMilliseconds / datasets.Count}ms");
        }

        [Test]
        public async Task ImportService_BatchProcessing_ProcessesInChunks()
        {
            // Arrange
            var testData = GenerateLargeTestDataset(10000);
            const int batchSize = 1000;
            var stopwatch = Stopwatch.StartNew();
            var totalProcessed = 0;
            
            // Act - Process in batches
            for (int i = 0; i < testData.Count; i += batchSize)
            {
                var batch = testData.Skip(i).Take(batchSize).ToList();
                var batchProcessed = 0;
                
                foreach (var record in batch)
                {
                    var validated = await _validationService.ValidateDataRowAsync(
                        CreateDataRow(record),
                        CreateTestFieldMappings()
                    );
                    
                    if (validated.IsValid)
                    {
                        batchProcessed++;
                    }
                }
                
                totalProcessed += batchProcessed;
                TestContext.WriteLine($"Batch {i / batchSize + 1}: Processed {batchProcessed} records");
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(totalProcessed, Is.EqualTo(10000));
            TestContext.WriteLine($"Total processing time: {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average batch time: {stopwatch.ElapsedMilliseconds / (testData.Count / batchSize)}ms");
        }

        [Test]
        public async Task ValidationService_LargeDataset_ValidatesEfficiently()
        {
            // Arrange
            var testData = GenerateLargeTestDataset(5000);
            var fieldMappings = CreateTestFieldMappings();
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var validCount = 0;
            foreach (var record in testData)
            {
                var result = await _validationService.ValidateDataRowAsync(
                    CreateDataRow(record),
                    fieldMappings
                );
                
                if (result.IsValid)
                {
                    validCount++;
                }
            }
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(validCount, Is.GreaterThan(0));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10000), 
                "Validation should complete within 10 seconds");
            
            TestContext.WriteLine($"Validated {validCount} records in {stopwatch.ElapsedMilliseconds}ms");
            TestContext.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / testData.Count:F2}ms per record");
        }

        #region Helper Methods

        private void CreateLargeExcelFile(string filePath, int recordCount)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("TestData");
            
            // Add headers
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Age";
            worksheet.Cells[1, 5].Value = "Score";
            worksheet.Cells[1, 6].Value = "IsActive";
            worksheet.Cells[1, 7].Value = "CreatedDate";
            
            // Add data rows
            var random = new Random(42); // Fixed seed for reproducibility
            for (int i = 0; i < recordCount; i++)
            {
                var row = i + 2; // Start from row 2 (after header)
                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = $"Record_{i}";
                worksheet.Cells[row, 3].Value = $"user{i}@example.com";
                worksheet.Cells[row, 4].Value = random.Next(18, 80);
                worksheet.Cells[row, 5].Value = random.NextDouble() * 100;
                worksheet.Cells[row, 6].Value = random.Next(0, 2) == 1;
                worksheet.Cells[row, 7].Value = DateTime.Now.AddDays(-random.Next(0, 365));
            }
            
            // Save the file
            var fileInfo = new FileInfo(filePath);
            package.SaveAs(fileInfo);
        }

        private List<TestRecord> GenerateLargeTestDataset(int count)
        {
            var records = new List<TestRecord>();
            var random = new Random(42); // Fixed seed for reproducibility
            
            for (int i = 0; i < count; i++)
            {
                records.Add(new TestRecord
                {
                    Id = i + 1,
                    Name = $"Record_{i}",
                    Email = $"user{i}@example.com",
                    Age = random.Next(18, 80),
                    Score = random.NextDouble() * 100,
                    IsActive = random.Next(0, 2) == 1,
                    CreatedDate = DateTime.Now.AddDays(-random.Next(0, 365))
                });
            }
            
            return records;
        }

        private System.Data.DataRow CreateDataRow(TestRecord record)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Age", typeof(int));
            table.Columns.Add("Score", typeof(double));
            table.Columns.Add("IsActive", typeof(bool));
            table.Columns.Add("CreatedDate", typeof(DateTime));
            
            var row = table.NewRow();
            row["Id"] = record.Id;
            row["Name"] = record.Name;
            row["Email"] = record.Email;
            row["Age"] = record.Age;
            row["Score"] = record.Score;
            row["IsActive"] = record.IsActive;
            row["CreatedDate"] = record.CreatedDate;
            
            return row;
        }

        private List<FieldMapping> CreateTestFieldMappings()
        {
            return new List<FieldMapping>
            {
                new FieldMapping { ExcelColumnName = "Id", DatabaseFieldName = "Id", IsRequired = true, DataType = "int" },
                new FieldMapping { ExcelColumnName = "Name", DatabaseFieldName = "Name", IsRequired = true, DataType = "string" },
                new FieldMapping { ExcelColumnName = "Email", DatabaseFieldName = "Email", IsRequired = true, DataType = "string" },
                new FieldMapping { ExcelColumnName = "Age", DatabaseFieldName = "Age", IsRequired = false, DataType = "int" },
                new FieldMapping { ExcelColumnName = "Score", DatabaseFieldName = "Score", IsRequired = false, DataType = "double" },
                new FieldMapping { ExcelColumnName = "IsActive", DatabaseFieldName = "IsActive", IsRequired = false, DataType = "boolean" },
                new FieldMapping { ExcelColumnName = "CreatedDate", DatabaseFieldName = "CreatedDate", IsRequired = false, DataType = "datetime" }
            };
        }

        private class TestRecord
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public int Age { get; set; }
            public double Score { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        #endregion
    }
}
