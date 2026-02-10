using System.Data;
using System.Globalization;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Services.Import
{
    public class ValidationService : IValidationService
    {
        public async Task<ValidationResult> ValidateDataRowAsync(DataRow row, List<FieldMapping> fieldMappings)
        {
            var result = new ValidationResult { IsValid = true };

            if (row == null)
            {
                result.IsValid = false;
                result.Errors.Add("Data row cannot be null");
                return result;
            }

            if (fieldMappings == null || !fieldMappings.Any())
            {
                result.IsValid = false;
                result.Errors.Add("Field mappings cannot be null or empty");
                return result;
            }

            foreach (var mapping in fieldMappings)
            {
                try
                {
                    var validationError = await ValidateFieldAsync(row, mapping);
                    if (!string.IsNullOrEmpty(validationError))
                    {
                        result.IsValid = false;
                        result.Errors.Add(validationError);
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Error validating field '{mapping.ExcelColumnName}': {ex.Message}");
                }
            }

            return result;
        }

        public async Task<ValidationResult> ValidateImportConfigurationAsync(ImportConfiguration config)
        {
            var result = new ValidationResult { IsValid = true };

            if (config == null)
            {
                result.IsValid = false;
                result.Errors.Add("Import configuration cannot be null");
                return result;
            }

            // Validate basic configuration properties
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Import configuration name is required");
            }

            if (string.IsNullOrWhiteSpace(config.TableName))
            {
                result.IsValid = false;
                result.Errors.Add("Target table name is required");
            }

            if (config.DatabaseConfigurationId <= 0)
            {
                result.IsValid = false;
                result.Errors.Add("Valid database configuration ID is required");
            }

            // Validate field mappings
            if (config.FieldMappings == null || !config.FieldMappings.Any())
            {
                result.IsValid = false;
                result.Errors.Add("At least one field mapping is required");
            }
            else
            {
                var requiredMappings = config.FieldMappings.Where(fm => fm.IsRequired).ToList();
                if (!requiredMappings.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add("At least one required field mapping must be defined");
                }

                // Check for duplicate Excel column names
                var duplicateColumns = config.FieldMappings
                    .GroupBy(fm => fm.ExcelColumnName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateColumns.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add($"Duplicate Excel column mappings found: {string.Join(", ", duplicateColumns)}");
                }

                // Check for duplicate database field names
                var duplicateFields = config.FieldMappings
                    .GroupBy(fm => fm.DatabaseFieldName)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateFields.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add($"Duplicate database field mappings found: {string.Join(", ", duplicateFields)}");
                }

                // Validate individual field mappings
                foreach (var mapping in config.FieldMappings)
                {
                    var mappingValidation = await ValidateFieldMappingAsync(mapping);
                    if (!mappingValidation.IsValid)
                    {
                        result.IsValid = false;
                        result.Errors.AddRange(mappingValidation.Errors);
                    }
                }
            }

            return result;
        }

        private async Task<string?> ValidateFieldAsync(DataRow row, FieldMapping mapping)
        {
            // Check if the Excel column exists in the data row
            if (!row.Table.Columns.Contains(mapping.ExcelColumnName))
            {
                return $"Excel column '{mapping.ExcelColumnName}' not found in data";
            }

            var cellValue = row[mapping.ExcelColumnName];

            // Check required field validation
            if (mapping.IsRequired && (cellValue == null || cellValue == DBNull.Value || string.IsNullOrWhiteSpace(cellValue.ToString())))
            {
                return $"Required field '{mapping.ExcelColumnName}' cannot be empty";
            }

            // If the field is not required and is empty, skip data type validation
            if (!mapping.IsRequired && (cellValue == null || cellValue == DBNull.Value || string.IsNullOrWhiteSpace(cellValue.ToString())))
            {
                return null;
            }

            // Validate data type
            var dataTypeValidationError = await ValidateDataTypeAsync(cellValue, mapping.DataType, mapping.ExcelColumnName);
            if (!string.IsNullOrEmpty(dataTypeValidationError))
            {
                return dataTypeValidationError;
            }

            return await Task.FromResult<string?>(null);
        }

        private async Task<ValidationResult> ValidateFieldMappingAsync(FieldMapping mapping)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(mapping.ExcelColumnName))
            {
                result.IsValid = false;
                result.Errors.Add("Excel column name is required for field mapping");
            }

            if (string.IsNullOrWhiteSpace(mapping.DatabaseFieldName))
            {
                result.IsValid = false;
                result.Errors.Add("Database field name is required for field mapping");
            }

            if (string.IsNullOrWhiteSpace(mapping.DataType))
            {
                result.IsValid = false;
                result.Errors.Add("Data type is required for field mapping");
            }
            else
            {
                // Validate that the data type is supported
                var supportedTypes = new[] { "string", "int", "decimal", "datetime", "bool", "guid" };
                if (!supportedTypes.Contains(mapping.DataType.ToLowerInvariant()))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Unsupported data type '{mapping.DataType}'. Supported types: {string.Join(", ", supportedTypes)}");
                }
            }

            return await Task.FromResult(result);
        }

        private async Task<string?> ValidateDataTypeAsync(object value, string dataType, string fieldName)
        {
            if (value == null || value == DBNull.Value)
                return null;

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            try
            {
                switch (dataType.ToLowerInvariant())
                {
                    case "string":
                        // String is always valid
                        break;

                    case "int":
                        if (!int.TryParse(stringValue, out _))
                        {
                            return $"Field '{fieldName}' value '{stringValue}' is not a valid integer";
                        }
                        break;

                    case "decimal":
                        if (!decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                        {
                            return $"Field '{fieldName}' value '{stringValue}' is not a valid decimal number";
                        }
                        break;

                    case "datetime":
                        if (!DateTime.TryParse(stringValue, out _))
                        {
                            return $"Field '{fieldName}' value '{stringValue}' is not a valid date/time";
                        }
                        break;

                    case "bool":
                        var lowerValue = stringValue.ToLowerInvariant();
                        if (lowerValue != "true" && lowerValue != "false" && lowerValue != "1" && lowerValue != "0" && 
                            lowerValue != "yes" && lowerValue != "no")
                        {
                            return $"Field '{fieldName}' value '{stringValue}' is not a valid boolean (true/false, 1/0, yes/no)";
                        }
                        break;

                    case "guid":
                        if (!Guid.TryParse(stringValue, out _))
                        {
                            return $"Field '{fieldName}' value '{stringValue}' is not a valid GUID";
                        }
                        break;

                    default:
                        return $"Unsupported data type '{dataType}' for field '{fieldName}'";
                }
            }
            catch (Exception ex)
            {
                return $"Error validating data type for field '{fieldName}': {ex.Message}";
            }

            return await Task.FromResult<string?>(null);
        }
    }
}