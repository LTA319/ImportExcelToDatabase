using System.Data;
using System.IO;
using ExcelDatabaseImportTool.Interfaces.Services;
using OfficeOpenXml;

namespace ExcelDatabaseImportTool.Services.Excel
{
    public class ExcelReaderService : IExcelReaderService
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

        public async Task<DataTable> ReadExcelFileAsync(string filePath)
        {
            SetLicense();
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // Validate file first (will throw detailed exception if invalid)
            await ValidateFileAsync(filePath);

            var dataTable = new DataTable();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                    throw new InvalidOperationException("No worksheets found in the Excel file.");

                var startRow = worksheet.Dimension?.Start.Row ?? 1;
                var endRow = worksheet.Dimension?.End.Row ?? 1;
                var startCol = worksheet.Dimension?.Start.Column ?? 1;
                var endCol = worksheet.Dimension?.End.Column ?? 1;

                // Create columns from first row (assuming header row)
                for (int col = startCol; col <= endCol; col++)
                {
                    var headerValue = worksheet.Cells[startRow, col].Value?.ToString() ?? $"Column{col}";
                    dataTable.Columns.Add(headerValue);
                }

                // Read data rows (skip header row)
                for (int row = startRow + 1; row <= endRow; row++)
                {
                    var dataRow = dataTable.NewRow();
                    for (int col = startCol; col <= endCol; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value;
                        dataRow[col - startCol] = cellValue ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }

                return await Task.FromResult(dataTable);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException || ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"Error reading Excel file: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetColumnNamesAsync(string filePath)
        {
            SetLicense();
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            // Validate file first (will throw detailed exception if invalid)
            await ValidateFileAsync(filePath);

            var columnNames = new List<string>();

            try
            {
                using var package = new ExcelPackage(new FileInfo(filePath));
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                    return columnNames;

                var startRow = worksheet.Dimension?.Start.Row ?? 1;
                var startCol = worksheet.Dimension?.Start.Column ?? 1;
                var endCol = worksheet.Dimension?.End.Column ?? 1;

                // Read column names from first row
                for (int col = startCol; col <= endCol; col++)
                {
                    var headerValue = worksheet.Cells[startRow, col].Value?.ToString() ?? $"Column{col}";
                    columnNames.Add(headerValue);
                }

                return await Task.FromResult(columnNames);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException || ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"Error reading column names from Excel file: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateFileAsync(string filePath)
        {
            SetLicense();
            
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Excel file not found: {filePath}");
                }

                // Check file extension
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    throw new InvalidOperationException($"Unsupported file format: {extension}. Only .xlsx and .xls files are supported.");
                }

                // Try to open the file to verify it's a valid Excel file
                using var package = new ExcelPackage(new FileInfo(filePath));
                
                // Check if there's at least one worksheet
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("The Excel file contains no worksheets.");
                }

                // Check if the first worksheet has data
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet?.Dimension == null)
                {
                    throw new InvalidOperationException("The first worksheet in the Excel file is empty.");
                }

                return await Task.FromResult(true);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read Excel file. The file may be corrupted or in an unsupported format. Error: {ex.Message}", ex);
            }
        }
    }
}