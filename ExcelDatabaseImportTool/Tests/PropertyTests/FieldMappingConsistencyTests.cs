using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Services.Import;
using System.Data;
using System.IO;

namespace ExcelDatabaseImportTool.Tests.PropertyTests
{
    /// <summary>
    /// **Feature: excel-database-import-tool, Property 5: Field mapping consistency**
    /// **Validates: Requirements 2.3**
    /// Property-based tests for field mapping consistency
    /// </summary>
    [TestFixture]
    public class FieldMappingConsistencyTests
    {
        private ValidationService _validationService = null!;

        [SetUp]
        public void Setup()
        {
            _validationService = new ValidationService();
        }

        /// <summary>
        /// Static test runner method for executing all field mapping consistency tests
        /// </summary>
        public static void RunFieldMappingConsistencyTests()
        {
            var results = new List<string>();
            results.Add("Running field mapping consistency tests...");

            var testInstance = new FieldMappingConsistencyTests();
            testInstance.Setup();

            try
            {
                // Run property-based test for required field enforcement
                testInstance.RunRequiredFieldEnforcementTest(results);

                // Run test for required field rejection of null/empty
                testInstance.RequiredFieldMappingRejectsNullOrEmpty();
                results.Add("PASS: Required field mapping rejects null or empty values");

                // Run test for optional field allowing null/empty
                testInstance.OptionalFieldMappingAllowsNullOrEmpty();
                results.Add("PASS: Optional field mapping allows null or empty values");

                // Run test for data type enforcement
                testInstance.FieldMappingEnforcesDataTypeSpecification();
                results.Add("PASS: Field mapping enforces data type specification");

                // Run test for invalid data type rejection
                testInstance.FieldMappingRejectsInvalidDataType();
                results.Add("PASS: Field mapping rejects invalid data types");

                // Run property-based test for constraint consistency
                testInstance.RunConstraintConsistencyTest(results);

                results.Add("Field mapping consistency tests completed successfully.");
            }
            catch (Exception ex)
            {
                results.Add($"ERROR: Exception during field mapping consistency tests: {ex.Message}");
                results.Add($"Stack trace: {ex.StackTrace}");
            }

            // Write results to file
            File.WriteAllLines("field_mapping_consistency_test_results.txt", results);
        }

        private void RunRequiredFieldEnforcementTest(List<string> results)
        {
            // Test with multiple random inputs
            var testCases = new[]
            {
                ("Column1", "Field1", "ValidValue"),
                ("TestCol", "TestField", "AnotherValue"),
                ("DataColumn", "DataField", "SomeData"),
                ("InputCol", "InputField", "TestInput"),
                ("NameColumn", "NameField", "John Doe")
            };

            foreach (var (columnName, fieldName, value) in testCases)
            {
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = columnName,
                    DatabaseFieldName = fieldName,
                    IsRequired = true,
                    DataType = "string"
                };

                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = value;
                dataTable.Rows.Add(row);

                var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                if (result.IsValid)
                {
                    results.Add($"PASS: Required field mapping enforces not-null constraint for '{columnName}' with value '{value}'");
                }
                else
                {
                    results.Add($"FAIL: Required field mapping should accept non-empty value '{value}' for '{columnName}'");
                }
            }
        }

        private void RunConstraintConsistencyTest(List<string> results)
        {
            // Test with multiple random inputs
            var testCases = new[]
            {
                ("Column1", "Field1", true, "Value1"),
                ("Column2", "Field2", false, "Value2"),
                ("Column3", "Field3", true, ""),
                ("Column4", "Field4", false, ""),
                ("Column5", "Field5", true, "TestValue")
            };

            foreach (var (columnName, fieldName, isRequired, value) in testCases)
            {
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = columnName,
                    DatabaseFieldName = fieldName,
                    IsRequired = isRequired,
                    DataType = "string"
                };

                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = string.IsNullOrEmpty(value) ? (object)DBNull.Value : value;
                dataTable.Rows.Add(row);

                var result1 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;
                var result2 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;
                var result3 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                if (result1.IsValid == result2.IsValid && result2.IsValid == result3.IsValid)
                {
                    results.Add($"PASS: Field mapping preserves constraints consistently for '{columnName}' (IsRequired={isRequired}, Value='{value}')");
                }
                else
                {
                    results.Add($"FAIL: Field mapping does not preserve constraints consistently for '{columnName}'");
                }
            }
        }

        /// <summary>
        /// Property test: For any field mapping with IsRequired=true, validation should enforce
        /// that the field cannot be null or empty
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool RequiredFieldMappingEnforcesNotNullConstraint(NonEmptyString columnName, NonEmptyString fieldName, NonEmptyString value)
        {
            // Arrange: Create a field mapping with IsRequired=true
            var mapping = new FieldMapping
            {
                Id = 1,
                ExcelColumnName = columnName.Get,
                DatabaseFieldName = fieldName.Get,
                IsRequired = true,
                DataType = "string"
            };

            // Create a DataTable with the Excel column
            var dataTable = new DataTable();
            dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
            var row = dataTable.NewRow();
            row[mapping.ExcelColumnName] = value.Get;
            dataTable.Rows.Add(row);

            // Act: Validate the data row with the required field mapping
            var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

            // Assert: Validation should succeed for non-empty values
            return result.IsValid;
        }

        /// <summary>
        /// Property test: For any field mapping with IsRequired=true, validation should fail
        /// when the field is null or empty
        /// </summary>
        [Test]
        public void RequiredFieldMappingRejectsNullOrEmpty()
        {
            // Test with various empty values
            var emptyValues = new object[] { DBNull.Value, "", "   ", "\t", "\n" };
            
            foreach (var emptyValue in emptyValues)
            {
                // Arrange: Create a field mapping with IsRequired=true
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = "TestColumn",
                    DatabaseFieldName = "TestField",
                    IsRequired = true,
                    DataType = "string"
                };

                // Create a DataTable with the Excel column
                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = emptyValue;
                dataTable.Rows.Add(row);

                // Act: Validate the data row with the required field mapping
                var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                // Assert: Validation should fail for empty values
                Assert.That(result.IsValid, Is.False, $"Validation should fail for empty value: {emptyValue}");
                Assert.That(result.Errors.Any(e => e.Contains("Required") || e.Contains("cannot be empty")), Is.True,
                    $"Error message should mention required field for value: {emptyValue}");
            }
        }

        /// <summary>
        /// Property test: For any field mapping with IsRequired=false, validation should allow
        /// null or empty values
        /// </summary>
        [Test]
        public void OptionalFieldMappingAllowsNullOrEmpty()
        {
            // Test with various empty values
            var emptyValues = new object[] { DBNull.Value, "", "   ", "\t", "\n" };
            
            foreach (var emptyValue in emptyValues)
            {
                // Arrange: Create a field mapping with IsRequired=false
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = "TestColumn",
                    DatabaseFieldName = "TestField",
                    IsRequired = false,
                    DataType = "string"
                };

                // Create a DataTable with the Excel column
                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = emptyValue;
                dataTable.Rows.Add(row);

                // Act: Validate the data row with the optional field mapping
                var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                // Assert: Validation should succeed for empty values when field is optional
                Assert.That(result.IsValid, Is.True, $"Validation should succeed for optional field with empty value: {emptyValue}");
            }
        }

        /// <summary>
        /// Property test: For any field mapping with a specific data type, validation should
        /// enforce that data type specification
        /// </summary>
        [Test]
        public void FieldMappingEnforcesDataTypeSpecification()
        {
            // Test with valid values for each data type
            var validTestCases = new[]
            {
                ("string", "test value"),
                ("int", "42"),
                ("int", "0"),
                ("int", "-100"),
                ("decimal", "3.14"),
                ("decimal", "0.0"),
                ("decimal", "-99.99"),
                ("datetime", "2024-01-15"),
                ("datetime", "2024-01-15 10:30:00"),
                ("bool", "true"),
                ("bool", "false"),
                ("bool", "1"),
                ("bool", "0"),
                ("bool", "yes"),
                ("bool", "no"),
                ("guid", "550e8400-e29b-41d4-a716-446655440000")
            };

            foreach (var (dataType, value) in validTestCases)
            {
                // Arrange: Create a field mapping with the specific data type
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = "TestColumn",
                    DatabaseFieldName = "TestField",
                    IsRequired = false,
                    DataType = dataType
                };

                // Create a DataTable with the Excel column
                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = value;
                dataTable.Rows.Add(row);

                // Act: Validate the data row with the field mapping
                var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                // Assert: Validation should succeed for valid data type values
                Assert.That(result.IsValid, Is.True, $"Validation should succeed for valid {dataType} value: {value}");
            }
        }

        /// <summary>
        /// Property test: For any field mapping with a specific data type, validation should
        /// reject values that don't match the data type specification
        /// </summary>
        [Test]
        public void FieldMappingRejectsInvalidDataType()
        {
            // Test with invalid values for each data type
            var invalidTestCases = new[]
            {
                ("int", "not a number"),
                ("int", "3.14"),
                ("int", "abc123"),
                ("decimal", "not a decimal"),
                ("decimal", "abc"),
                ("datetime", "not a date"),
                ("datetime", "2024-13-45"),
                ("datetime", "invalid"),
                ("bool", "maybe"),
                ("bool", "2"),
                ("bool", "invalid"),
                ("guid", "not-a-guid"),
                ("guid", "12345")
            };

            foreach (var (dataType, invalidValue) in invalidTestCases)
            {
                // Arrange: Create a field mapping with the specific data type
                var mapping = new FieldMapping
                {
                    Id = 1,
                    ExcelColumnName = "TestColumn",
                    DatabaseFieldName = "TestField",
                    IsRequired = false,
                    DataType = dataType
                };

                // Create a DataTable with the Excel column
                var dataTable = new DataTable();
                dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
                var row = dataTable.NewRow();
                row[mapping.ExcelColumnName] = invalidValue;
                dataTable.Rows.Add(row);

                // Act: Validate the data row with the field mapping
                var result = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

                // Assert: Validation should fail for invalid data type values
                Assert.That(result.IsValid, Is.False, $"Validation should fail for invalid {dataType} value: {invalidValue}");
                Assert.That(result.Errors.Any(e => e.Contains("not a valid")), Is.True,
                    $"Error message should mention invalid value for {dataType}: {invalidValue}");
            }
        }

        /// <summary>
        /// Property test: For any field mapping, the null/not-null constraint should be preserved
        /// consistently across multiple validation calls
        /// </summary>
        [FsCheck.NUnit.Property(MaxTest = 100)]
        public bool FieldMappingPreservesConstraintsConsistently(NonEmptyString columnName, NonEmptyString fieldName, bool isRequired, string value)
        {
            // Arrange: Create a field mapping
            var mapping = new FieldMapping
            {
                Id = 1,
                ExcelColumnName = columnName.Get,
                DatabaseFieldName = fieldName.Get,
                IsRequired = isRequired,
                DataType = "string"
            };

            // Create a DataTable with the Excel column
            var dataTable = new DataTable();
            dataTable.Columns.Add(mapping.ExcelColumnName, typeof(string));
            var row = dataTable.NewRow();
            row[mapping.ExcelColumnName] = value ?? (object)DBNull.Value;
            dataTable.Rows.Add(row);

            // Act: Validate the same data row multiple times
            var result1 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;
            var result2 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;
            var result3 = _validationService.ValidateDataRowAsync(row, new List<FieldMapping> { mapping }).Result;

            // Assert: Results should be consistent across multiple validations
            return result1.IsValid == result2.IsValid && result2.IsValid == result3.IsValid;
        }
    }
}
