using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Dapper_Extensions.Crud.Extensions;

public static class SqlServerEnumMappingExtensions
{
    /// <summary>
    ///     Registers a custom Dapper type handler for the specified enum type,
    ///     so that enums are stored and retrieved as strings in SQL Server.
    /// </summary>
    public static IServiceCollection AddSqlServerEnumMapping<TEnum>(this IServiceCollection services)
        where TEnum : struct, Enum
    {
        SqlMapper.AddTypeHandler(new SqlServerEnumTypeHandler<TEnum>());
        return services;
    }
}