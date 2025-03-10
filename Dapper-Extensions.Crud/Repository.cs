using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using Npgsql;
using Dapper_Extensions.Crud.Interfaces;
using Dapper_Extensions.Crud.Interfaces.DapperCrudLibrary.Mapping; // Your mapping interfaces

namespace Dapper_Extensions.Crud
{
    public class Repository<T>(IDbConnection connection, IDbTransaction? transaction) : IRepository<T>, ITransactionRepository where T : class
    {
        private readonly IDbConnection _connection = connection;
        public IDbTransaction? Transaction { get; set; } = transaction;

        #region Asynchronous CRUD

        public async Task<T> GetByIdAsync(int id)
        {
            return await _connection.GetAsync<T>(id);
        }

        public T GetById(int id)
        {
            return connection.Get<T>(id);
        }

        public IEnumerable<T> GetAll()
        {
            return connection.GetAll<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _connection.GetAllAsync<T>();
        }

        public async Task<long> AddAsync(T entity)
        {
            return await _connection.InsertAsync(entity);
        }
        public long Add(T entity)
        {
            return connection.Insert(entity);
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            return await _connection.UpdateAsync(entity);
        }

        public bool Update(T entity)
        {
            return connection.Update(entity);
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            return await _connection.DeleteAsync(entity);
        }

        public bool Delete(T entity)
        {
            return connection.Delete(entity);
        }
        #endregion

        #region Asynchronous Stored Procedures

        // Executes a stored procedure that returns data.
        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcName, object parameters = null)
        {
            return await _connection.QueryAsync<TResult>(
                storedProcName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<TResult> ExecuteStoredProcedure<TResult>(string storedProcName, object parameters = null)
        {
            return connection.Query<TResult>(
                storedProcName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // Executes a stored procedure that does not return data.
        public async Task<int> ExecuteStoredProcedureAsync(string storedProcName, object parameters = null)
        {
            return await _connection.ExecuteAsync(
                storedProcName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public int ExecuteStoredProcedure(string storedProcName, object parameters = null)
        {
            return connection.Execute(
                storedProcName,
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        #endregion

        #region Asynchronous Upsert

        public async Task<long> UpsertAsync(T entity)
        {
            // Find properties marked with [Key]
            var keyProperties = typeof(T)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute)))
                .ToList();

            if (!keyProperties.Any())
                throw new Exception("No property with [Key] attribute found in type " + typeof(T).Name);

            if (keyProperties.Count == 1)
            {
                // Single key logic:
                var keyProp = keyProperties.First();
                var keyValue = keyProp.GetValue(entity);

                if (IsDefault(keyValue) || await GetByIdAsync(Convert.ToInt32(keyValue)) == null)
                    return await AddAsync(entity);

                var updated = await UpdateAsync(entity);
                if (!updated)
                    return await AddAsync(entity);

                return Convert.ToInt64(keyValue);
            }

            // Composite key logic:
            // Determine table name from [Table] attribute; if missing, use class name.
            var tableAttr = (TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var tableName = tableAttr?.Name ?? typeof(T).Name;

            // Build WHERE clause using composite key properties.
            var whereClauses = keyProperties.Select(p => $"{p.Name} = @{p.Name}");
            var whereClause = string.Join(" AND ", whereClauses);
            var query = $"SELECT * FROM {tableName} WHERE {whereClause}";

            // Build parameters.
            var parameters = new DynamicParameters();
            foreach (var prop in keyProperties)
                parameters.Add("@" + prop.Name, prop.GetValue(entity));

            // Check for existing record.
            var results = await _connection.QueryAsync<T>(query, parameters, Transaction);
            var existing = results.FirstOrDefault();

            if (existing == null)
                return await AddAsync(entity);
            else
            {
                var updated = await UpdateAsync(entity);
                return !updated ? await AddAsync(entity) : 0;
            }
        }
        // Upsert method for single or composite key entities.
        public long Upsert(T entity)
        {
            // Find properties marked with [Key]
            var keyProperties = typeof(T)
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(KeyAttribute)))
                .ToList();

            if (!keyProperties.Any())
            {
                throw new Exception("No property with [Key] attribute found in type " + typeof(T).Name);
            }

            if (keyProperties.Count == 1)
            {
                // Single key logic:
                var keyProp = keyProperties.First();
                var keyValue = keyProp.GetValue(entity);

                if (IsDefault(keyValue) || GetById(Convert.ToInt32(keyValue)) == null)
                {
                    return Add(entity);
                }

                var updated = Update(entity);
                if (!updated)
                {
                    return Add(entity);
                }

                return Convert.ToInt64(keyValue);
            }

            // Composite key logic:
            // Determine table name from [Table] attribute; if missing, use class name.
            var tableAttr = (TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var tableName = tableAttr?.Name ?? typeof(T).Name;

            // Build WHERE clause using composite key properties.
            var whereClauses = keyProperties.Select(p => $"{p.Name} = @{p.Name}");
            var whereClause = string.Join(" AND ", whereClauses);
            var query = $"SELECT * FROM {tableName} WHERE {whereClause}";

            // Build parameters.
            var parameters = new DynamicParameters();
            foreach (var prop in keyProperties)
                parameters.Add("@" + prop.Name, prop.GetValue(entity));

            // Check for existing record.
            var existing = connection.Query<T>(query, parameters, Transaction).FirstOrDefault();

            if (existing == null)
            {
                return Add(entity);
            }
            else
            {
                var updated = Update(entity);
                return !updated ? Add(entity) :
                    // For composite keys, return 0 to indicate an update.
                    0;
            }
        }
        /// <summary>
        /// Performs a batched upsert of a list of entities, splitting them into batches.
        /// Returns a tuple with the total inserted and updated counts.
        /// Supports SQL Server (MERGE with OUTPUT) and PostgreSQL (INSERT ... ON CONFLICT with RETURNING).
        /// </summary>
        public async Task<(int inserted, int updated)> UpsertListBatchAsync(IEnumerable<T> entities, int batchSize = 1000)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            if (!entityList.Any())
                return (0, 0);

            int insertedCount = 0;
            int updatedCount = 0;

            // Process entities in batches.
            for (int i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var batchResult = await ExecuteBatchUpsertAsync(batch);
                insertedCount += batchResult.inserted;
                updatedCount += batchResult.updated;
            }

            return (insertedCount, updatedCount);
        }

        /// <summary>
        /// Performs a batched upsert of a list of entities, splitting them into batches.
        /// Returns a tuple with the total inserted and updated counts.
        /// Supports SQL Server (MERGE with OUTPUT) and PostgreSQL (INSERT ... ON CONFLICT with RETURNING).
        /// </summary>
        public (int inserted, int updated) UpsertListBatch(IEnumerable<T> entities, int batchSize = 1000)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return (0, 0);
            }

            var insertedCount = 0;
            var updatedCount = 0;

            // Process entities in batches.
            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var batchResult = ExecuteBatchUpsert(batch);
                insertedCount += batchResult.inserted;
                updatedCount += batchResult.updated;
            }

            return (insertedCount, updatedCount);
        }

        /// <summary>
        /// Executes the batched upsert for a single batch and returns counts for inserted and updated rows.
        /// </summary>
        private async Task<(int inserted, int updated)> ExecuteBatchUpsertAsync(List<T> entityBatch)
        {
            // Retrieve mapping from the global registry.
            var mapping = EntityMappingRegistry.GetMapping<T>();
            var tableName = mapping.TableName;

            // Get public properties (assumed to match DB column names).
            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.Name).ToList();

            // Determine key and non-key columns from mapping.
            var keyColumns = mapping.KeyProperties.ToList();
            var nonKeyColumns = columnNames.Except(keyColumns).ToList();

            // Build the VALUES clause with unique parameter names.
            var valuesList = new List<string>();
            var parameters = new DynamicParameters();

            for (int i = 0; i < entityBatch.Count; i++)
            {
                var entity = entityBatch[i];
                var valuePlaceholders = new List<string>();
                foreach (var prop in properties)
                {
                    var paramName = $"{prop.Name}_{i}";
                    valuePlaceholders.Add("@" + paramName);
                    parameters.Add(paramName, prop.GetValue(entity));
                }
                valuesList.Add("(" + string.Join(", ", valuePlaceholders) + ")");
            }

            var valuesClause = string.Join(", ", valuesList);
            string sql;

            if (_connection is SqlConnection)
            {
                // SQL Server: Build MERGE statement with OUTPUT clause.
                var sourceColumns = string.Join(", ", columnNames);
                var onConditions = keyColumns.Select(k => $"Target.{k} = Source.{k}");
                var onClause = string.Join(" AND ", onConditions);
                var updateSetClause = string.Join(", ", nonKeyColumns.Select(c => $"Target.{c} = Source.{c}"));
                var insertColumns = string.Join(", ", columnNames);
                var insertValues = string.Join(", ", columnNames.Select(c => $"Source.{c}"));

                sql = $@"
MERGE INTO {tableName} AS Target
USING (VALUES {valuesClause}) AS Source({sourceColumns})
ON {onClause}
WHEN MATCHED THEN 
    UPDATE SET {updateSetClause}
WHEN NOT MATCHED THEN
    INSERT ({insertColumns}) VALUES ({insertValues})
OUTPUT $action AS Action;";
            }
            else if (_connection is NpgsqlConnection)
            {
                // PostgreSQL: Build INSERT ... ON CONFLICT with RETURNING clause.
                var insertColumns = string.Join(", ", columnNames);
                var conflictColumns = string.Join(", ", keyColumns);
                var updateSetClause = string.Join(", ", nonKeyColumns.Select(c => $"{c} = EXCLUDED.{c}"));
                sql = $@"
INSERT INTO {tableName} ({insertColumns})
VALUES {valuesClause}
ON CONFLICT ({conflictColumns}) DO UPDATE
SET {updateSetClause}
RETURNING (CASE WHEN xmax = 0 THEN 'INSERT' ELSE 'UPDATE' END) AS Action;";
            }
            else
            {
                throw new NotSupportedException("Batch upsert is supported only for SQL Server and PostgreSQL.");
            }

            // Execute the query and count the returned actions.
            var actions = (await _connection.QueryAsync<string>(sql, parameters, Transaction)).ToList();
            var insertedCount = actions.Count(a => a.Equals("INSERT", StringComparison.OrdinalIgnoreCase));
            var updatedCount = actions.Count(a => a.Equals("UPDATE", StringComparison.OrdinalIgnoreCase));

            return (insertedCount, updatedCount);
        }

        /// <summary>
        /// Executes the batched upsert for a single batch and returns counts for inserted and updated rows.
        /// </summary>
        private (int inserted, int updated) ExecuteBatchUpsert(List<T> entityBatch)
        {
            // Retrieve mapping from the global registry.
            var mapping = EntityMappingRegistry.GetMapping<T>();
            var tableName = mapping.TableName;

            // Get public properties (assumed to match DB column names).
            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.Name).ToList();

            // Determine key and non-key columns from mapping.
            var keyColumns = mapping.KeyProperties.ToList();
            var nonKeyColumns = columnNames.Except(keyColumns).ToList();

            // Build VALUES clause with unique parameter names.
            var valuesList = new List<string>();
            var parameters = new DynamicParameters();

            for (var i = 0; i < entityBatch.Count; i++)
            {
                var entity = entityBatch[i];
                var valuePlaceholders = new List<string>();
                foreach (var prop in properties)
                {
                    var paramName = $"{prop.Name}_{i}";
                    valuePlaceholders.Add("@" + paramName);
                    parameters.Add(paramName, prop.GetValue(entity));
                }
                valuesList.Add("(" + string.Join(", ", valuePlaceholders) + ")");
            }

            var valuesClause = string.Join(", ", valuesList);
            string sql;

            if (connection is SqlConnection)
            {
                // SQL Server: Build MERGE statement with OUTPUT clause.
                var sourceColumns = string.Join(", ", columnNames);
                var onConditions = keyColumns.Select(k => $"Target.{k} = Source.{k}");
                var onClause = string.Join(" AND ", onConditions);
                var updateSetClause = string.Join(", ", nonKeyColumns.Select(c => $"Target.{c} = Source.{c}"));
                var insertColumns = string.Join(", ", columnNames);
                var insertValues = string.Join(", ", columnNames.Select(c => $"Source.{c}"));

                sql = $@"
MERGE INTO {tableName} AS Target
USING (VALUES {valuesClause}) AS Source({sourceColumns})
ON {onClause}
WHEN MATCHED THEN 
    UPDATE SET {updateSetClause}
WHEN NOT MATCHED THEN
    INSERT ({insertColumns}) VALUES ({insertValues})
OUTPUT $action AS Action;";
            }
            else if (connection is NpgsqlConnection)
            {
                // PostgreSQL: Build INSERT ... ON CONFLICT with RETURNING clause.
                var insertColumns = string.Join(", ", columnNames);
                var conflictColumns = string.Join(", ", keyColumns);
                var updateSetClause = string.Join(", ", nonKeyColumns.Select(c => $"{c} = EXCLUDED.{c}"));
                sql = $@"
INSERT INTO {tableName} ({insertColumns})
VALUES {valuesClause}
ON CONFLICT ({conflictColumns}) DO UPDATE
SET {updateSetClause}
RETURNING (CASE WHEN xmax = 0 THEN 'INSERT' ELSE 'UPDATE' END) AS Action;";
            }
            else
            {
                throw new NotSupportedException("Batch upsert is supported only for SQL Server and PostgreSQL.");
            }

            // Execute the query and count the returned actions.
            var actions = connection.Query<string>(sql, parameters, Transaction).ToList();
            var insertedCount = actions.Count(a => a.Equals("INSERT", StringComparison.OrdinalIgnoreCase));
            var updatedCount = actions.Count(a => a.Equals("UPDATE", StringComparison.OrdinalIgnoreCase));

            return (insertedCount, updatedCount);
        }

        // Helper method to determine if a value is the default for its type.

        #endregion

        #region Helper Method

        // Helper method to determine if a value is the default for its type.
        private bool IsDefault(object? value)
        {
            if (value == null)
                return true;

            var type = value.GetType();
            return value.Equals(Activator.CreateInstance(type));
        }

        #endregion
    }
}
