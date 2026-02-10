using System.Data;
using System.IO;
using ExcelDatabaseImportTool.Services.Import;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 11: Data validation integrity**
    /// **Validates: Requirements 5.1**
    /// </summary>
    public static class DataValidationTests
    {
        public static void RunDataValidationTests()
        {
            var results = new List<string>();
            results.Add("Running data validation integrity tests...");

            var validationService = new ValidationService();
            
            try
            {
                // Test required field validation
                TestRequiredFieldValidation(validationService, results);
                
                // Test data type validation
                TestDataTypeValidation(validationService, results);
                
                // Test import configuration validation
                TestImportConfigurationValidation(validationService, results);
                
                // Test edge cases
                TestEdgeCases(validationService, results);
                
                results.Add("Data validation integrity tests completed.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during data validation tests: {ex.Message}");
            }
            
            // Write results to file
            File.WriteAllLines("data_validation_test_results.txt", results);
        }

        private static void TestRequiredFieldValidation(ValidationService service, List<string> results)
        {
            try
            {
                // Create test data table
                var dataTable = new DataTable();
                dataTable.Columns.Add("RequiredField", typeof(string));
                dataTable.Columns.Add("OptionalField", typeof(string));

                // Test cases: required field with value, required field empty, optional field empty
                var testCases = new[]
                {
                    new { RequiredValue = "Valid Value", OptionalValue = "Optional Value", ShouldPass = true, Description = "Both fields have values" },
                    new { RequiredValue = "", OptionalValue = "Optional Value", ShouldPass = false, Description = "Required field is empty" },
                    new { RequiredValue = "Valid Value", OptionalValue = "", ShouldPass = true, Description = "Optional field is empty but required field has value" },
                    new { RequiredValue = (string?)null, OptionalValue = "Optional Value", ShouldPass = false, Description = "Required field is null" },
                    new { RequiredValue = "   ", OptionalValue = "Optional Value", ShouldPass = false, Description = "Required field is whitespace only" }
                };

                var fieldMappings = new List<FieldMapping>
                {
                    new FieldMapping
                    {
                        ExcelColumnName = "RequiredField",
                        DatabaseFieldName = "RequiredField",
                        IsRequired = true,
                        DataType = "string"
                    },
                    new FieldMapping
                    {
                        ExcelColumnName = "OptionalField",
                        DatabaseFieldName = "OptionalField",
                        IsRequired = false,
                        DataType = "string"
                    }
                };

                foreach (var testCase in testCases)
                {
                    var row = dataTable.NewRow();
                    row["RequiredField"] = testCase.RequiredValue ?? (object)DBNull.Value;
                    row["OptionalField"] = testCase.OptionalValue ?? (object)DBNull.Value;
                    dataTable.Rows.Add(row);

                    var validationResult = service.ValidateDataRowAsync(row, fieldMappings).Result;

                    if (validationResult.IsValid == testCase.ShouldPass)
                    {
                        results.Add($"PASS: Required field validation - {testCase.Description}");
                    }
                    else
                    {
                        results.Add($"FAIL: Required field validation - {testCase.Description}");
                        results.Add($"  Expected: {testCase.ShouldPass}, Actual: {validationResult.IsValid}");
                        if (validationResult.Errors.Any())
                        {
                            results.Add($"  Errors: {string.Join("; ", validationResult.Errors)}");
                        }
                    }

                    dataTable.Rows.Remove(row);
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during required field validation test: {ex.Message}");
            }
        }

        private static void TestDataTypeValidation(ValidationService service, List<string> results)
        {
            try
            {
                // Create test data table
                var dataTable = new DataTable();
                dataTable.Columns.Add("IntField", typeof(string));
                dataTable.Columns.Add("DecimalField", typeof(string));
                dataTable.Columns.Add("DateField", typeof(string));
                dataTable.Columns.Add("BoolField", typeof(string));

                // Test cases for different data types
                var testCases = new[]
                {
                    // Valid cases
                    new { IntValue = "123", DecimalValue = "123.45", DateValue = "2024-01-01", BoolValue = "true", ShouldPass = true, Description = "All valid values" },
                    new { IntValue = "0", DecimalValue = "0.0", DateValue = "2024-12-31", BoolValue = "false", ShouldPass = true, Description = "Valid boundary values" },
                    new { IntValue = "-123", DecimalValue = "-123.45", DateValue = "1/1/2024", BoolValue = "1", ShouldPass = true, Description = "Valid alternative formats" },
                    
                    // Invalid cases
                    new { IntValue = "abc", DecimalValue = "123.45", DateValue = "2024-01-01", BoolValue = "true", ShouldPass = false, Description = "Invalid integer" },
                    new { IntValue = "123", DecimalValue = "abc", DateValue = "2024-01-01", BoolValue = "true", ShouldPass = false, Description = "Invalid decimal" },
                    new { IntValue = "123", DecimalValue = "123.45", DateValue = "invalid-date", BoolValue = "true", ShouldPass = false, Description = "Invalid date" },
                    new { IntValue = "123", DecimalValue = "123.45", DateValue = "2024-01-01", BoolValue = "maybe", ShouldPass = false, Description = "Invalid boolean" }
                };

                var fieldMappings = new List<FieldMapping>
                {
                    new FieldMapping { ExcelColumnName = "IntField", DatabaseFieldName = "IntField", IsRequired = true, DataType = "int" },
                    new FieldMapping { ExcelColumnName = "DecimalField", DatabaseFieldName = "DecimalField", IsRequired = true, DataType = "decimal" },
                    new FieldMapping { ExcelColumnName = "DateField", DatabaseFieldName = "DateField", IsRequired = true, DataType = "datetime" },
                    new FieldMapping { ExcelColumnName = "BoolField", DatabaseFieldName = "BoolField", IsRequired = true, DataType = "bool" }
                };

                foreach (var testCase in testCases)
                {
                    var row = dataTable.NewRow();
                    row["IntField"] = testCase.IntValue;
                    row["DecimalField"] = testCase.DecimalValue;
                    row["DateField"] = testCase.DateValue;
                    row["BoolField"] = testCase.BoolValue;
                    dataTable.Rows.Add(row);

                    var validationResult = service.ValidateDataRowAsync(row, fieldMappings).Result;

                    if (validationResult.IsValid == testCase.ShouldPass)
                    {
                        results.Add($"PASS: Data type validation - {testCase.Description}");
                    }
                    else
                    {
                        results.Add($"FAIL: Data type validation - {testCase.Description}");
                        results.Add($"  Expected: {testCase.ShouldPass}, Actual: {validationResult.IsValid}");
                        if (validationResult.Errors.Any())
                        {
                            results.Add($"  Errors: {string.Join("; ", validationResult.Errors)}");
                        }
                    }

                    dataTable.Rows.Remove(row);
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during data type validation test: {ex.Message}");
            }
        }

        private static void TestImportConfigurationValidation(ValidationService service, List<string> results)
        {
            try
            {
                // Test valid configuration
                var validConfig = new ImportConfiguration
                {
                    Name = "Valid Config",
                    DatabaseConfigurationId = 1,
                    TableName = "Users",
                    HasHeaderRow = true,
                    FieldMappings = new List<FieldMapping>
                    {
                        new FieldMapping
                        {
                            ExcelColumnName = "Name",
                            DatabaseFieldName = "FullName",
                            IsRequired = true,
                            DataType = "string"
                        },
                        new FieldMapping
                        {
                            ExcelColumnName = "Email",
                            DatabaseFieldName = "EmailAddress",
                            IsRequired = true,
                            DataType = "string"
                        }
                    }
                };

                var validResult = service.ValidateImportConfigurationAsync(validConfig).Result;
                if (validResult.IsValid)
                {
                    results.Add("PASS: Valid import configuration correctly validated");
                }
                else
                {
                    results.Add("FAIL: Valid import configuration incorrectly rejected");
                    results.Add($"  Errors: {string.Join("; ", validResult.Errors)}");
                }

                // Test invalid configurations
                var invalidConfigs = new[]
                {
                    new { Config = new ImportConfiguration { Name = "", DatabaseConfigurationId = 1, TableName = "Users", FieldMappings = new List<FieldMapping>() }, Description = "Empty name" },
                    new { Config = new ImportConfiguration { Name = "Test", DatabaseConfigurationId = 0, TableName = "Users", FieldMappings = new List<FieldMapping>() }, Description = "Invalid database config ID" },
                    new { Config = new ImportConfiguration { Name = "Test", DatabaseConfigurationId = 1, TableName = "", FieldMappings = new List<FieldMapping>() }, Description = "Empty table name" },
                    new { Config = new ImportConfiguration { Name = "Test", DatabaseConfigurationId = 1, TableName = "Users", FieldMappings = null }, Description = "Null field mappings" }
                };

                foreach (var testCase in invalidConfigs)
                {
                    var invalidResult = service.ValidateImportConfigurationAsync(testCase.Config).Result;
                    if (!invalidResult.IsValid)
                    {
                        results.Add($"PASS: Invalid import configuration correctly rejected - {testCase.Description}");
                    }
                    else
                    {
                        results.Add($"FAIL: Invalid import configuration incorrectly accepted - {testCase.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during import configuration validation test: {ex.Message}");
            }
        }

        private static void TestEdgeCases(ValidationService service, List<string> results)
        {
            try
            {
                // Test null data row
                var nullRowResult = service.ValidateDataRowAsync(null, new List<FieldMapping>()).Result;
                if (!nullRowResult.IsValid)
                {
                    results.Add("PASS: Null data row correctly rejected");
                }
                else
                {
                    results.Add("FAIL: Null data row incorrectly accepted");
                }

                // Test empty field mappings
                var dataTable = new DataTable();
                dataTable.Columns.Add("TestField", typeof(string));
                var row = dataTable.NewRow();
                row["TestField"] = "Test Value";
                dataTable.Rows.Add(row);

                var emptyMappingsResult = service.ValidateDataRowAsync(row, new List<FieldMapping>()).Result;
                if (!emptyMappingsResult.IsValid)
                {
                    results.Add("PASS: Empty field mappings correctly rejected");
                }
                else
                {
                    results.Add("FAIL: Empty field mappings incorrectly accepted");
                }

                // Test missing Excel column
                var fieldMappings = new List<FieldMapping>
                {
                    new FieldMapping
                    {
                        ExcelColumnName = "NonExistentColumn",
                        DatabaseFieldName = "TestField",
                        IsRequired = true,
                        DataType = "string"
                    }
                };

                var missingColumnResult = service.ValidateDataRowAsync(row, fieldMappings).Result;
                if (!missingColumnResult.IsValid)
                {
                    results.Add("PASS: Missing Excel column correctly detected");
                }
                else
                {
                    results.Add("FAIL: Missing Excel column not detected");
                }
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during edge case validation test: {ex.Message}");
            }
        }
    }
}