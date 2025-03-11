using System.Data;

namespace Dapper_Extensions.Crud.Interfaces;

public interface ITransactionRepository
{
    IDbTransaction? Transaction { get; set; }
}