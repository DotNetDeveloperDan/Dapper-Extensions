using System.Data;

namespace Dapper_Extensions.Crud.Interfaces;

public interface IDapperExecutor
{
    // These operations use Dapper.Contrib and require a reference type.
    Task<T> GetAsync<T>(IDbConnection connection, object id, IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class;

    T Get<T>(IDbConnection connection, object id, IDbTransaction transaction = null, int? commandTimeout = null)
        where T : class;

    Task<IEnumerable<T>> GetAllAsync<T>(IDbConnection connection, IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class;

    IEnumerable<T> GetAll<T>(IDbConnection connection, IDbTransaction transaction = null, int? commandTimeout = null)
        where T : class;

    Task<long> InsertAsync<T>(IDbConnection connection, T entity, IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class;

    long Insert<T>(IDbConnection connection, T entity, IDbTransaction transaction = null, int? commandTimeout = null)
        where T : class;

    Task<bool> UpdateAsync<T>(IDbConnection connection, T entity, IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class;

    bool Update<T>(IDbConnection connection, T entity, IDbTransaction transaction = null, int? commandTimeout = null)
        where T : class;

    Task<bool> DeleteAsync<T>(IDbConnection connection, T entity, IDbTransaction transaction = null,
        int? commandTimeout = null) where T : class;

    bool Delete<T>(IDbConnection connection, T entity, IDbTransaction transaction = null, int? commandTimeout = null)
        where T : class;

    // These operations use Dapper's Query methods which can work with any type.
    Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType);

    IEnumerable<T> Query<T>(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        bool buffered,
        int commandTimeout,
        CommandType? commandType);

    Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType);

    int Execute(
        IDbConnection connection,
        string sql,
        object param,
        IDbTransaction transaction,
        int? commandTimeout,
        CommandType? commandType);
}