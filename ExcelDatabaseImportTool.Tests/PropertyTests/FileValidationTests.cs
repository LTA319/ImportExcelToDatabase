using System.Data;
using System.IO;
using ExcelDatabaseImportTool.Services.Excel;
using OfficeOpenXml;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 7: File validation reliability**
    /// **Validates: Requirements 3.2**
    /// </summary>
    public static class FileValidationTests
    {
        private static void SetLicense()
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
        public static void RunFileValidationTests()
        {
            var results = new List<string>();
            results.Add("Running file validation tests...");

            var excelReaderService = new ExcelReaderService();
            var testDirectory = Path.Combine(Path.GetTempPath(), "ExcelValidationTests");
            
            try
            {
                Directory.CreateDirectory(testDirectory);

                // Test valid Excel files
                TestValidExcelFiles(excelReaderService, testDirectory, results);
                
                // Test invalid files
                TestInvalidFiles(excelReaderService, testDirectory, results);
                
                // Test non-existent files
                TestNonExistentFiles(excelReaderService, testDirectory, results);
                
                // Test empty Excel files
                TestEmptyExcelFiles(excelReaderService, testDirectory, results);
                
                results.Add("File validation tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during file validation tests: {ex.Message}");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDirectory))
                {
                    try
                    {
                        Directory.Delete(testDirectory, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            
            // Write results to file
            File.WriteAllLines("file_validation_test_results.txt", results);
        }

        private static void TestValidExcelFiles(ExcelReaderService service, string testDirectory, List<string> results)
        {
            var validExtensions = new[] { ".xlsx", ".xls" };
            
            foreach (var ext in validExtensions)
            {
                try
                {
                    var validFilePath = Path.Combine(testDirectory, $"valid{ext}");
                    CreateValidExcelFile(validFilePath);
                    
                    var result = service.ValidateFileAsync(validFilePath).Result;
                    
                    if (result)
                    {
                        results.Add($"PASS: Valid Excel file with extension {ext} correctly validated as true");
                    }
                    else
                    {
                        results.Add($"FAIL: Valid Excel file with extension {ext} incorrectly validated as false");
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception testing valid file with extension {ext}: {ex.Message}");
                }
            }
        }

        private static void TestInvalidFiles(ExcelReaderService service, string testDirectory, List<string> results)
        {
            var invalidExtensions = new[] { ".txt", ".csv", ".doc", ".pdf" };
            
            foreach (var ext in invalidExtensions)
            {
                try
                {
                    var invalidFilePath = Path.Combine(testDirectory, $"invalid{ext}");
                    File.WriteAllText(invalidFilePath, "This is not an Excel file");
                    
                    var result = service.ValidateFileAsync(invalidFilePath).Result;
                    
                    if (!result)
                    {
                        results.Add($"PASS: Invalid file with extension {ext} correctly validated as false");
                    }
                    else
                    {
                        results.Add($"FAIL: Invalid file with extension {ext} incorrectly validated as true");
                    }
                }
                catch (Exception ex)
                {
                    results.Add($"ERROR: Exception testing invalid file with extension {ext}: {ex.Message}");
                }
            }
        }

        private static void TestNonExistentFiles(ExcelReaderService service, string testDirectory, List<string> results)
        {
            try
            {
                var nonExistentPath = Path.Combine(testDirectory, "nonexistent.xlsx");
                var result = service.ValidateFileAsync(nonExistentPath).Result;
                
                if (!result)
                {
                    results.Add("PASS: Non-existent file correctly validated as false");
                }
                else
                {
                    results.Add("FAIL: Non-existent file incorrectly validated as true");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception testing non-existent file: {ex.Message}");
            }
        }

        private static void TestEmptyExcelFiles(ExcelReaderService service, string testDirectory, List<string> results)
        {
            try
            {
                var emptyExcelPath = Path.Combine(testDirectory, "empty.xlsx");
                CreateEmptyExcelFile(emptyExcelPath);
                
                var result = service.ValidateFileAsync(emptyExcelPath).Result;
                
                if (!result)
                {
                    results.Add("PASS: Empty Excel file correctly validated as false");
                }
                else
                {
                    results.Add("FAIL: Empty Excel file incorrectly validated as true");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception testing empty Excel file: {ex.Message}");
            }
        }

        private static void CreateValidExcelFile(string filePath)
        {
            SetLicense();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("TestSheet");
            
            // Add some sample data
            worksheet.Cells[1, 1].Value = "Column1";
            worksheet.Cells[1, 2].Value = "Column2";
            worksheet.Cells[2, 1].Value = "Data1";
            worksheet.Cells[2, 2].Value = "Data2";
            
            package.SaveAs(new FileInfo(filePath));
        }

        private static void CreateEmptyExcelFile(string filePath)
        {
            SetLicense();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("EmptySheet");
            // Don't add any data - this should make validation fail
            package.SaveAs(new FileInfo(filePath));
        }
    }
}