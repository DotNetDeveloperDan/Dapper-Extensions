using System.Data;
using Dapper_Extensions.Crud.Enums;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Dapper_Extensions.Crud;

public class DbConnectionFactory
{
    private readonly string _connectionString;
    private readonly DatabaseProvider _provider;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        _provider = DetermineProviderFromConnectionString(_connectionString);
    }

    public IDbConnection CreateConnection()
    {
        return _provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(_connectionString),
            DatabaseProvider.PostgreSql => new NpgsqlConnection(_connectionString),
            _ => throw new NotSupportedException("Database provider not supported.")
        };
    }

    // Heuristic method to determine the database provider.
    private DatabaseProvider DetermineProviderFromConnectionString(string connectionString)
    {
        if (connectionString.IndexOf("Host=", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DatabaseProvider.PostgreSql;
        }

        if (connectionString.IndexOf("Server=", StringComparison.OrdinalIgnoreCase) >= 0 ||
            connectionString.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return DatabaseProvider.SqlServer;
        }

        throw new NotSupportedException("Unable to determine database provider from the connection string.");
    }
}