using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Interfaces.Repositories;
using ExcelDatabaseImportTool.Models.Configuration;
using ExcelDatabaseImportTool.Models.Domain;
using System.Data;
using System.Data.Common;
using System.IO;

namespace ExcelDatabaseImportTool.Services.Import
{
    public class ImportService : IImportService
    {
        private readonly IExcelReaderService _excelReaderService;
        private readonly IValidationService _validationService;
        private readonly IForeignKeyResolverService _foreignKeyResolverService;
        private readonly IDatabaseConnectionService _databaseConnectionService;
        private readonly IImportLogRepository _importLogRepository;
        private readonly IConfigurationRepository _configurationRepository;

        public event EventHandler<ImportProgressEventArgs>? ProgressUpdated;

        public ImportService(
            IExcelReaderService excelReaderService,
            IValidationService validationService,
            IForeignKeyResolverService foreignKeyResolverService,
            IDatabaseConnectionService databaseConnectionService,
            IImportLogRepository importLogRepository,
            IConfigurationRepository configurationRepository)
        {
            _excelReaderService = excelReaderService;
            _validationService = validationService;
            _foreignKeyResolverService = foreignKeyResolverService;
            _databaseConnectionService = databaseConnectionService;
            _importLogRepository = importLogRepository;
            _configurationRepository = configurationRepository;
        }

        public async Task<ImportResult> ImportDataAsync(ImportConfiguration config, string excelFilePath, CancellationToken cancellationToken = default)
        {
            var result = new ImportResult();
            var importLog = new ImportLog
            {
                ImportConfigurationId = config.Id,
                ExcelFileName = Path.GetFileName(excelFilePath),
                StartTime = DateTime.UtcNow,
                Status = ImportStatus.Failed
            };

            try
            {
                // Save initial log entry
                await _importLogRepository.SaveImportLogAsync(importLog);
                result.ImportLog = importLog;

                // Get database configuration
                var dbConfig = await _configurationRepository.GetDatabaseConfigurationByIdAsync(config.DatabaseConfigurationId);
                if (dbConfig == null)
                {
                    throw new InvalidOperationException($"Database configuration with ID {config.DatabaseConfigurationId} not found");
                }

                // Validate Excel file
                if (!await _excelReaderService.ValidateFileAsync(excelFilePath))
                {
                    throw new InvalidOperationException("Invalid Excel file format");
                }

                // Read Excel data
                var dataTable = await _excelReaderService.ReadExcelFileAsync(excelFilePath);
                if (dataTable.Rows.Count == 0)
                {
                    result.Success = true;
                    importLog.Status = ImportStatus.Success;
                    importLog.EndTime = DateTime.UtcNow;
                    await _importLogRepository.UpdateImportLogAsync(importLog);
                    return result;
                }

                result.TotalRecords = dataTable.Rows.Count;
                importLog.TotalRecords = result.TotalRecords;

                // Process data in batches with transaction management
                await ProcessDataInBatches(dataTable, config, dbConfig, result, importLog, cancellationToken);

                // Update final status
                if (result.FailedRecords == 0)
                {
                    importLog.Status = ImportStatus.Success;
                    result.Success = true;
                }
                else if (result.SuccessfulRecords > 0)
                {
                    importLog.Status = ImportStatus.Partial;
                    result.Success = true;
                }
                else
                {
                    importLog.Status = ImportStatus.Failed;
                    result.Success = false;
                }

                importLog.EndTime = DateTime.UtcNow;
                importLog.SuccessfulRecords = result.SuccessfulRecords;
                importLog.FailedRecords = result.FailedRecords;
                importLog.ErrorDetails = string.Join(Environment.NewLine, result.Errors);

                await _importLogRepository.UpdateImportLogAsync(importLog);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
                
                importLog.Status = ImportStatus.Failed;
                importLog.EndTime = DateTime.UtcNow;
                importLog.ErrorDetails = ex.Message;
                
                await _importLogRepository.UpdateImportLogAsync(importLog);
            }

            return result;
        }

        private async Task ProcessDataInBatches(DataTable dataTable, ImportConfiguration config, DatabaseConfiguration dbConfig, 
            ImportResult result, ImportLog importLog, CancellationToken cancellationToken)
        {
            const int batchSize = 1000; // Process in batches of 1000 records
            var totalRows = dataTable.Rows.Count;
            var processedRows = 0;

            using var connection = await _databaseConnectionService.CreateConnectionAsync(dbConfig);
            connection.Open();

            for (int startIndex = 0; startIndex < totalRows; startIndex += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var endIndex = Math.Min(startIndex + batchSize, totalRows);
                var batchRows = dataTable.Rows.Cast<DataRow>().Skip(startIndex).Take(endIndex - startIndex).ToList();

                await ProcessBatch(batchRows, config, dbConfig, connection, result, cancellationToken);

                processedRows += batchRows.Count;
                
                // Report progress
                ProgressUpdated?.Invoke(this, new ImportProgressEventArgs
                {
                    ProcessedRecords = processedRows,
                    TotalRecords = totalRows,
                    CurrentOperation = $"Processing batch {startIndex / batchSize + 1}",
                    CanCancel = true
                });
            }
        }

        private async Task ProcessBatch(List<DataRow> batchRows, ImportConfiguration config, DatabaseConfiguration dbConfig, 
            IDbConnection connection, ImportResult result, CancellationToken cancellationToken)
        {
            using var transaction = connection.BeginTransaction();
            var batchErrors = new List<string>();
            var batchSuccessCount = 0;

            try
            {
                foreach (var row in batchRows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await ProcessSingleRow(row, config, dbConfig, connection, transaction, cancellationToken);
                        batchSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        var rowIndex = batchRows.IndexOf(row) + 1;
                        var errorMessage = $"Row {result.SuccessfulRecords + result.FailedRecords + rowIndex}: {ex.Message}";
                        batchErrors.Add(errorMessage);
                    }
                }

                // Commit the entire batch if no errors occurred
                if (batchErrors.Count == 0)
                {
                    transaction.Commit();
                    result.SuccessfulRecords += batchSuccessCount;
                }
                else
                {
                    // Rollback the entire batch if any errors occurred
                    transaction.Rollback();
                    result.FailedRecords += batchRows.Count;
                    result.Errors.AddRange(batchErrors);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                result.FailedRecords += batchRows.Count;
                result.Errors.Add($"Batch processing failed: {ex.Message}");
            }
        }

        private async Task ProcessSingleRow(DataRow row, ImportConfiguration config, DatabaseConfiguration dbConfig, 
            IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken)
        {
            // Validate the row data
            var validationResult = await _validationService.ValidateDataRowAsync(row, config.FieldMappings);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Resolve foreign keys
            var foreignKeyMappings = config.FieldMappings
                .Where(fm => fm.ForeignKeyMapping != null)
                .Select(fm => fm.ForeignKeyMapping!)
                .ToList();

            var foreignKeyValues = new Dictionary<string, object>();
            if (foreignKeyMappings.Any())
            {
                var lookupValues = new Dictionary<string, string>();
                foreach (var mapping in foreignKeyMappings)
                {
                    var fieldMapping = config.FieldMappings.First(fm => fm.ForeignKeyMapping == mapping);
                    var cellValue = row[fieldMapping.ExcelColumnName]?.ToString() ?? string.Empty;
                    lookupValues[fieldMapping.DatabaseFieldName] = cellValue;
                }

                foreignKeyValues = await _foreignKeyResolverService.ResolveForeignKeysAsync(lookupValues, foreignKeyMappings, dbConfig);
            }

            // Build and execute insert command
            await ExecuteInsertCommand(row, config, connection, transaction, foreignKeyValues, cancellationToken);
        }

        private async Task ExecuteInsertCommand(DataRow row, ImportConfiguration config, IDbConnection connection, 
            IDbTransaction transaction, Dictionary<string, object> foreignKeyValues, CancellationToken cancellationToken)
        {
            var fieldNames = new List<string>();
            var parameterNames = new List<string>();
            var parameters = new List<IDbDataParameter>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            foreach (var fieldMapping in config.FieldMappings)
            {
                fieldNames.Add($"[{fieldMapping.DatabaseFieldName}]");
                parameterNames.Add($"@{fieldMapping.DatabaseFieldName}");

                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{fieldMapping.DatabaseFieldName}";

                // Use foreign key value if available, otherwise use Excel cell value
                if (foreignKeyValues.ContainsKey(fieldMapping.DatabaseFieldName))
                {
                    parameter.Value = foreignKeyValues[fieldMapping.DatabaseFieldName] ?? DBNull.Value;
                }
                else
                {
                    var cellValue = row[fieldMapping.ExcelColumnName];
                    parameter.Value = ConvertCellValue(cellValue, fieldMapping.DataType);
                }

                parameters.Add(parameter);
                command.Parameters.Add(parameter);
            }

            var sql = $"INSERT INTO [{config.TableName}] ({string.Join(", ", fieldNames)}) VALUES ({string.Join(", ", parameterNames)})";
            command.CommandText = sql;

            command.ExecuteNonQuery();
        }

        private object ConvertCellValue(object? cellValue, string dataType)
        {
            if (cellValue == null || cellValue == DBNull.Value || string.IsNullOrWhiteSpace(cellValue.ToString()))
            {
                return DBNull.Value;
            }

            var stringValue = cellValue.ToString()!;

            return dataType.ToLowerInvariant() switch
            {
                "int" or "integer" => int.TryParse(stringValue, out var intVal) ? intVal : DBNull.Value,
                "bigint" or "long" => long.TryParse(stringValue, out var longVal) ? longVal : DBNull.Value,
                "decimal" or "numeric" => decimal.TryParse(stringValue, out var decVal) ? decVal : DBNull.Value,
                "float" or "real" => float.TryParse(stringValue, out var floatVal) ? floatVal : DBNull.Value,
                "double" => double.TryParse(stringValue, out var doubleVal) ? doubleVal : DBNull.Value,
                "bit" or "boolean" => bool.TryParse(stringValue, out var boolVal) ? boolVal : DBNull.Value,
                "datetime" or "date" => DateTime.TryParse(stringValue, out var dateVal) ? dateVal : DBNull.Value,
                "guid" or "uniqueidentifier" => Guid.TryParse(stringValue, out var guidVal) ? guidVal : DBNull.Value,
                _ => stringValue
            };
        }
    }
}