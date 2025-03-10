using System.Data;

namespace Dapper_Extensions.Crud.Interfaces;

internal interface ITransactionRepository
{
    IDbTransaction? Transaction { get; set; }
}