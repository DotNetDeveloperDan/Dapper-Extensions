namespace Dapper_Extensions.Crud.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    void BeginTransaction();
    void Commit();
    void Rollback();
}