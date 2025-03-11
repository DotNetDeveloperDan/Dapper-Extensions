namespace Dapper_Extensions.Crud.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(object id);
    T GetById(object id);
    Task<IEnumerable<T>> GetAllAsync();
    IEnumerable<T> GetAll();
    Task<long> AddAsync(T entity);
    long Add(T entity);
    Task<bool> UpdateAsync(T entity);
    bool Update(T entity);
    Task<bool> DeleteAsync(T entity);
    bool Delete(T entity);
    Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcName, object parameters);
    IEnumerable<TResult> ExecuteStoredProcedure<TResult>(string storedProcName, object parameters);
    Task<int> ExecuteStoredProcedureAsync(string storedProcName, object parameters);
    int ExecuteStoredProcedure(string storedProcName, object parameters);
    Task<long> UpsertAsync(T entity);
    long Upsert(T entity);
    Task<(int inserted, int updated)> UpsertListBatchAsync(IEnumerable<T> entities, int batchSize);
    (int inserted, int updated) UpsertListBatch(IEnumerable<T> entities, int batchSize);
}