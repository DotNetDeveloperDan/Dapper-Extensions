using System.Data;
using Dapper_Extensions.Crud.Interfaces;

namespace Dapper_Extensions.Crud
{
    public class UnitOfWork(IDbConnection connection) : IUnitOfWork, IDisposable
    {
        private readonly IDbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        private readonly Dictionary<Type, object> _repositories = new();
        private IDbTransaction? _transaction;

        // Retrieve a repository for the given entity type.
        public IRepository<T> Repository<T>() where T : class
        {
            if (!_repositories.TryGetValue(typeof(T), out var repo))
            {
                repo = new Repository<T>(_connection, _transaction);
                if (_transaction != null && repo is ITransactionRepository txRepo)
                {
                    txRepo.Transaction = _transaction;
                }
                _repositories[typeof(T)] = repo;
            }

            return (IRepository<T>)repo;
        }

        // Begin a transaction and update all existing repositories.
        public void BeginTransaction()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            _transaction = _connection.BeginTransaction();

            // Assign the transaction to all cached repositories.
            foreach (var repo in _repositories.Values)
            {
                if (repo is ITransactionRepository txRepo)
                {
                    txRepo.Transaction = _transaction;
                }
            }
        }

        // Commit the transaction and clear it from all repositories.
        public void Commit()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;

            // Clear transaction references in all repositories.
            foreach (var repo in _repositories.Values)
            {
                if (repo is ITransactionRepository txRepo)
                {
                    txRepo.Transaction = null;
                }
            }
        }

        // Rollback the transaction and clear it from all repositories.
        public void Rollback()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to roll back.");
            }

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;

            // Clear transaction references in all repositories.
            foreach (var repo in _repositories.Values)
            {
                if (repo is ITransactionRepository txRepo)
                {
                    txRepo.Transaction = null;
                }
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
            }
            _connection.Dispose();
        }
    }
}
