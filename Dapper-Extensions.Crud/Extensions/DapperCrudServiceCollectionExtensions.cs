using System.Data;
using Dapper_Extensions.Crud.Enums;
using Dapper_Extensions.Crud.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Dapper_Extensions.Crud.Extensions;

public static class DapperCrudServiceCollectionExtensions
{
    /// <summary>
    ///     Registers Dapper CRUD services including the IDbConnection,
    ///     repository, and unit of work. The consumer can choose the database provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider to use.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDapperCrud(this IServiceCollection services, string connectionString,
        DatabaseProvider provider)
    {
        services.AddScoped<IDbConnection>(sp =>
        {
            IDbConnection connection = provider switch
            {
                DatabaseProvider.SqlServer => new SqlConnection(connectionString),
                DatabaseProvider.PostgreSql => new NpgsqlConnection(connectionString),
                _ => throw new NotSupportedException("The specified database provider is not supported.")
            };

            connection.Open();
            return connection;
        });

        // Register the generic repository and unit of work.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}