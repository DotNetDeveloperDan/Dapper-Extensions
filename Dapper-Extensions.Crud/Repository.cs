using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper_Extensions.Crud.Enums;
using Dapper_Extensions.Crud.Interfaces;
using Dapper_Extensions.Crud.Interfaces.DapperCrudLibrary.Mapping;

namespace Dapper_Extensions.Crud
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IDbConnection _connection;
        private readonly IDapperExecutor _executor;
        private readonly DatabaseProvider _provider;
        // For simplicity, transactions are not supported in these examples.
        private IDbTransaction? Transaction => null;

        public Repository(IDbConnection connection, IDapperExecutor executor, DatabaseProvider provider = DatabaseProvider.SqlServer)
        {
            _connection = connection;
            _executor = executor;
            _provider = provider;
        }

        #region Helper Method

        // Helper method to determine if a value is the default for its type.
        private bool IsDefault(object? value)
        {
            if (value == null)
            {
                return true;
            }
            var type = value.GetType();
            return value.Equals(Activator.CreateInstance(type));
        }

        #endregion

        #region Asynchronous CRUD

        public async Task<T> GetByIdAsync(object id)
        {
            return await _executor.GetAsync<T>(_connection, id);
        }

        public T GetById(object id)
        {
            return _executor.Get<T>(_connection, id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _executor.GetAllAsync<T>(_connection);
        }

        public IEnumerable<T> GetAll()
        {
            return _executor.GetAll<T>(_connection);
        }

        public async Task<long> AddAsync(T entity)
        {
            return await _executor.InsertAsync(_connection, entity);
        }

        public long Add(T entity)
        {
            return _executor.Insert(_connection, entity);
        }

        public async Task<bool> UpdateAsync(T entity)
        {
            return await _executor.UpdateAsync(_connection, entity);
        }

        public bool Update(T entity)
        {
            return _executor.Update(_connection, entity);
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            return await _executor.DeleteAsync(_connection, entity);
        }

        public bool Delete(T entity)
        {
            return _executor.Delete(_connection, entity);
        }

        #endregion

        #region Stored Procedures

        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcName, object parameters)
        {
            return await _executor.QueryAsync<TResult>(_connection, storedProcName, parameters, null, null, CommandType.StoredProcedure);
        }

        public IEnumerable<TResult> ExecuteStoredProcedure<TResult>(string storedProcName, object parameters)
        {
            return _executor.Query<TResult>(_connection, storedProcName, parameters, null, true, 0, CommandType.StoredProcedure);
        }

        public async Task<int> ExecuteStoredProcedureAsync(string storedProcName, object parameters)
        {
            return await _executor.ExecuteAsync(_connection, storedProcName, parameters, null, null, CommandType.StoredProcedure);
        }

        public int ExecuteStoredProcedure(string storedProcName, object parameters)
        {
            return _executor.Execute(_connection, storedProcName, parameters, null, null, CommandType.StoredProcedure);
        }

        #endregion

        #region Upsert

        public async Task<long> UpsertAsync(T entity)
        {
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
                var keyProp = keyProperties.First();
                var keyValue = keyProp.GetValue(entity);

                if (IsDefault(keyValue) || await GetByIdAsync(Convert.ToInt32(keyValue)) == null)
                {
                    return await AddAsync(entity);
                }

                var updated = await UpdateAsync(entity);
                if (!updated)
                {
                    return await AddAsync(entity);
                }

                return Convert.ToInt64(keyValue);
            }

            // Composite key logic:
            var tableAttr = (TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var tableName = tableAttr?.Name ?? typeof(T).Name;

            var whereClauses = keyProperties.Select(p => $"{p.Name} = @{p.Name}");
            var whereClause = string.Join(" AND ", whereClauses);
            var query = $"SELECT * FROM {tableName} WHERE {whereClause}";

            var parameters = new DynamicParameters();
            foreach (var prop in keyProperties)
                parameters.Add("@" + prop.Name, prop.GetValue(entity));

            var results = await _executor.QueryAsync<T>(_connection, query, parameters, Transaction, null, CommandType.Text);
            var existing = results.FirstOrDefault();

            if (existing == null)
            {
                return await AddAsync(entity);
            }
            else
            {
                var updated = await UpdateAsync(entity);
                return !updated ? await AddAsync(entity) : 0;
            }
        }

        public long Upsert(T entity)
        {
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

            var tableAttr = (TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
            var tableName = tableAttr?.Name ?? typeof(T).Name;

            var whereClauses = keyProperties.Select(p => $"{p.Name} = @{p.Name}");
            var whereClause = string.Join(" AND ", whereClauses);
            var query = $"SELECT * FROM {tableName} WHERE {whereClause}";

            var parameters = new DynamicParameters();
            foreach (var prop in keyProperties)
                parameters.Add("@" + prop.Name, prop.GetValue(entity));

            var existing = _executor.Query<T>(_connection, query, parameters, Transaction, true, 0, CommandType.Text).FirstOrDefault();

            if (existing == null)
            {
                return Add(entity);
            }
            else
            {
                var updated = Update(entity);
                return !updated ? Add(entity) : 0;
            }
        }

        public async Task<(int inserted, int updated)> UpsertListBatchAsync(IEnumerable<T> entities, int batchSize = 1000)
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

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var batchResult = await ExecuteBatchUpsertAsync(batch);
                insertedCount += batchResult.inserted;
                updatedCount += batchResult.updated;
            }

            return (insertedCount, updatedCount);
        }

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

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var batchResult = ExecuteBatchUpsert(batch);
                insertedCount += batchResult.inserted;
                updatedCount += batchResult.updated;
            }

            return (insertedCount, updatedCount);
        }

        private async Task<(int inserted, int updated)> ExecuteBatchUpsertAsync(List<T> entityBatch)
        {
            var mapping = EntityMappingRegistry.GetMapping<T>();
            var tableName = mapping.TableName;

            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.Name).ToList();
            var keyColumns = mapping.KeyProperties.ToList();
            var nonKeyColumns = columnNames.Except(keyColumns).ToList();

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

            if (_provider == DatabaseProvider.SqlServer)
            {
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
            else if (_provider == DatabaseProvider.PostgreSql)
            {
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

            // Now use the executor to perform the query.
            var actions = (await _executor.QueryAsync<string>(_connection, sql, parameters, Transaction, null, CommandType.Text)).ToList();
            var insertedCount = actions.Count(a => a.Equals("INSERT", StringComparison.OrdinalIgnoreCase));
            var updatedCount = actions.Count(a => a.Equals("UPDATE", StringComparison.OrdinalIgnoreCase));

            return (insertedCount, updatedCount);
        }

        private (int inserted, int updated) ExecuteBatchUpsert(List<T> entityBatch)
        {
            var mapping = EntityMappingRegistry.GetMapping<T>();
            var tableName = mapping.TableName;

            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.Name).ToList();
            var keyColumns = mapping.KeyProperties.ToList();
            var nonKeyColumns = columnNames.Except(keyColumns).ToList();

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

            if (_provider == DatabaseProvider.SqlServer)
            {
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
            else if (_provider == DatabaseProvider.PostgreSql)
            {
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

            var actions = _executor.Query<string>(_connection, sql, parameters, Transaction, true, 0, CommandType.Text).ToList();
            var insertedCount = actions.Count(a => a.Equals("INSERT", StringComparison.OrdinalIgnoreCase));
            var updatedCount = actions.Count(a => a.Equals("UPDATE", StringComparison.OrdinalIgnoreCase));

            return (insertedCount, updatedCount);
        }

        #endregion
    }
}
