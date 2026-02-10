using System.Collections.Concurrent;
using System.Data;
using ExcelDatabaseImportTool.Interfaces.Services;
using ExcelDatabaseImportTool.Models.Configuration;

namespace ExcelDatabaseImportTool.Services.Import
{
    public class ForeignKeyResolverService : IForeignKeyResolverService
    {
        private readonly IDatabaseConnectionService _connectionService;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _cache;
        private readonly TimeSpan _cacheExpiry;
        private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;

        public ForeignKeyResolverService(IDatabaseConnectionService connectionService)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object?>>();
            _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
            _cacheExpiry = TimeSpan.FromMinutes(30); // Cache expires after 30 minutes
        }

        public async Task<object?> ResolveForeignKeyAsync(string lookupValue, ForeignKeyMapping mapping, DatabaseConfiguration dbConfig)
        {
            if (string.IsNullOrWhiteSpace(lookupValue))
                return null;

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            if (dbConfig == null)
                throw new ArgumentNullException(nameof(dbConfig));

            // Create cache key based on database config, table, and lookup field
            var cacheKey = $"{dbConfig.Id}_{mapping.ReferencedTable}_{mapping.ReferencedLookupField}";
            
            // Check if cache exists and is not expired
            if (_cache.TryGetValue(cacheKey, out var tableCache) && 
                _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
                DateTime.UtcNow - timestamp < _cacheExpiry)
            {
                if (tableCache.TryGetValue(lookupValue, out var cachedResult))
                {
                    return cachedResult;
                }
            }

            // If not in cache or cache expired, query the database
            try
            {
                using var connection = await _connectionService.CreateConnectionAsync(dbConfig);
                using var command = connection.CreateCommand();

                // Build parameterized query to prevent SQL injection
                command.CommandText = $"SELECT {EscapeIdentifier(mapping.ReferencedKeyField)} FROM {EscapeIdentifier(mapping.ReferencedTable)} WHERE {EscapeIdentifier(mapping.ReferencedLookupField)} = @lookupValue";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@lookupValue";
                parameter.Value = lookupValue;
                command.Parameters.Add(parameter);

                var result = await ExecuteScalarAsync(command);

                // Update cache
                if (!_cache.ContainsKey(cacheKey))
                {
                    _cache[cacheKey] = new ConcurrentDictionary<string, object?>();
                    _cacheTimestamps[cacheKey] = DateTime.UtcNow;
                }

                _cache[cacheKey][lookupValue] = result;

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve foreign key for value '{lookupValue}' in table '{mapping.ReferencedTable}' using lookup field '{mapping.ReferencedLookupField}': {ex.Message}", 
                    ex);
            }
        }

        public async Task<Dictionary<string, object>> ResolveForeignKeysAsync(Dictionary<string, string> lookupValues, List<ForeignKeyMapping> mappings, DatabaseConfiguration dbConfig)
        {
            if (lookupValues == null)
                throw new ArgumentNullException(nameof(lookupValues));

            if (mappings == null)
                throw new ArgumentNullException(nameof(mappings));

            if (dbConfig == null)
                throw new ArgumentNullException(nameof(dbConfig));

            var results = new Dictionary<string, object>();
            var errors = new List<string>();

            foreach (var kvp in lookupValues)
            {
                var fieldName = kvp.Key;
                var lookupValue = kvp.Value;

                // Find the corresponding mapping for this field
                var mapping = mappings.FirstOrDefault(m => 
                    // This assumes the field name matches somehow - you might need to adjust this logic
                    // based on how the mappings are structured in your application
                    m.ReferencedTable != null);

                if (mapping == null)
                {
                    errors.Add($"No foreign key mapping found for field '{fieldName}'");
                    continue;
                }

                try
                {
                    var resolvedValue = await ResolveForeignKeyAsync(lookupValue, mapping, dbConfig);
                    if (resolvedValue != null)
                    {
                        results[fieldName] = resolvedValue;
                    }
                    else
                    {
                        errors.Add($"No foreign key found for value '{lookupValue}' in field '{fieldName}'");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error resolving foreign key for field '{fieldName}': {ex.Message}");
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException($"Foreign key resolution failed: {string.Join("; ", errors)}");
            }

            return results;
        }

        private string EscapeIdentifier(string identifier)
        {
            // Basic identifier escaping - you might want to make this database-specific
            return $"`{identifier.Replace("`", "``")}`";
        }

        private async Task<object?> ExecuteScalarAsync(IDbCommand command)
        {
            // Handle async execution based on command type
            return command switch
            {
                Microsoft.Data.SqlClient.SqlCommand sqlCommand => await sqlCommand.ExecuteScalarAsync(),
                MySql.Data.MySqlClient.MySqlCommand mysqlCommand => await mysqlCommand.ExecuteScalarAsync(),
                _ => command.ExecuteScalar()
            };
        }

        public void ClearCache()
        {
            _cache.Clear();
            _cacheTimestamps.Clear();
        }

        public void ClearCache(string cacheKey)
        {
            _cache.TryRemove(cacheKey, out _);
            _cacheTimestamps.TryRemove(cacheKey, out _);
        }
    }
}