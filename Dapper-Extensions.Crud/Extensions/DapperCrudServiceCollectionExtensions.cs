using System;
using System.Data;
using Dapper_Extensions.Crud.Enums;
using Dapper_Extensions.Crud.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Dapper_Extensions.Crud.Extensions;

public static class DapperCrudServiceCollectionExtensions
{
    /// <summary>
    /// Registers Dapper CRUD services including the IDbConnection,
    /// repository, and unit of work. The consumer can choose the database provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="provider">The database provider to use.</param>
    /// <param name="enumMappings">Optional mappings for enums (used with PostgreSQL).</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDapperCrud(this IServiceCollection services,
          string connectionString,
          DatabaseProvider provider,
          IDictionary<Type, string>? enumMappings = null)
    {
        services.AddScoped<IDbConnection>(sp =>
        {
            // Get the logger from the container.
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DapperCrudServiceCollectionExtensions");
            try
            {
                IDbConnection connection = provider switch
                {
                    DatabaseProvider.SqlServer => new SqlConnection(connectionString),
                    DatabaseProvider.PostgreSql => new NpgsqlConnection(connectionString),
                    _ => throw new NotSupportedException("Unsupported provider")
                };

                // If the connection is an NpgsqlConnection and enum mappings are provided,
                // register each mapping on this connection.
                if (connection is NpgsqlConnection npgsqlConnection && enumMappings != null)
                {
                    foreach (var mapping in enumMappings)
                    {
                        // Look up the RegisterEnumMapping method that takes a string argument.
                        var registerMethod = typeof(NpgsqlConnection)
                            .GetMethod("RegisterEnumMapping", new Type[] { typeof(string) });
                        if (registerMethod != null)
                        {
                            // Make the method generic for the enum type.
                            var genericMethod = registerMethod.MakeGenericMethod(mapping.Key);
                            genericMethod.Invoke(npgsqlConnection, new object[] { mapping.Value });
                        }
                    }
                }

                connection.Open();
                logger.LogInformation("Opened {Provider} connection successfully.", provider);
                return connection;
            }
            catch (NotSupportedException ex)
            {
                logger.LogError(ex, "Database provider {Provider} is not supported.", provider);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating the database connection for provider {Provider}.", provider);
                throw;
            }
        });

        // Register the Dapper executor implementation.
        services.AddScoped<IDapperExecutor, DapperExecutor>();

        // Register the repository and unit of work.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
