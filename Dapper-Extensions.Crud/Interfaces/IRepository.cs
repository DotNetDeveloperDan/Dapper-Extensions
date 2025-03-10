namespace Dapper_Extensions.Crud.Interfaces;

public interface IRepository<T> where T : class
{
    public Task<T> GetByIdAsync(int id);
    public T GetById(int id);
    public Task<IEnumerable<T>> GetAllAsync();
    IEnumerable<T> GetAll();
    public Task<long> AddAsync(T entity);
    public long Add(T entity);
    public Task<bool> UpdateAsync(T entity);
    public bool Update(T entity);
    public Task<bool> DeleteAsync(T entity);
    public bool Delete(T entity);
    public Task<int> ExecuteStoredProcedureAsync(string storedProcName, object parameters = null);
    public int ExecuteStoredProcedure(string storedProcName, object parameters = null);
    public Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcName, object parameters = null);
    public IEnumerable<TResult> ExecuteStoredProcedure<TResult>(string storedProcName, object parameters = null);
    public Task<long> UpsertAsync(T entity);
    public long Upsert(T entity);
    public Task<(int inserted, int updated)> UpsertListBatchAsync(IEnumerable<T> entities, int batchSize = 1000);
    public (int inserted, int updated) UpsertListBatch(IEnumerable<T> entities, int batchSize = 1000);
}