using System.Data;
using Dapper_Extensions.Crud.Interfaces;
using Dapper;
using Dapper.Contrib.Extensions;

namespace Dapper_Extensions.Crud;

public class DapperExecutor : IDapperExecutor
{
    public async Task<T> GetAsync<T>(
        IDbConnection connection,
        object id,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return await connection.GetAsync<T>(id, transaction, commandTimeout);
    }

    public T Get<T>(
        IDbConnection connection,
        object id,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return connection.Get<T>(id, transaction, commandTimeout);
    }

    public async Task<IEnumerable<T>> GetAllAsync<T>(
        IDbConnection connection,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return await connection.GetAllAsync<T>(transaction, commandTimeout);
    }

    public IEnumerable<T> GetAll<T>(
        IDbConnection connection,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return connection.GetAll<T>(transaction, commandTimeout);
    }

    public async Task<long> InsertAsync<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return await connection.InsertAsync(entity, transaction, commandTimeout);
    }

    public long Insert<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return connection.Insert(entity, transaction, commandTimeout);
    }

    public async Task<bool> UpdateAsync<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return await connection.UpdateAsync(entity, transaction, commandTimeout);
    }

    public bool Update<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return connection.Update(entity, transaction, commandTimeout);
    }

    public async Task<bool> DeleteAsync<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return await connection.DeleteAsync(entity, transaction, commandTimeout);
    }

    public bool Delete<T>(
        IDbConnection connection,
        T entity,
        IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class
    {
        return connection.Delete(entity, transaction, commandTimeout);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType)
    {
        return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public IEnumerable<T> Query<T>(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        bool buffered,
        int commandTimeout,
        CommandType? commandType)
    {
        return connection.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
    }

    public async Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType)
    {
        return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
    }

    public int Execute(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType)
    {
        return connection.Execute(sql, param, transaction, commandTimeout, commandType);
    }
}