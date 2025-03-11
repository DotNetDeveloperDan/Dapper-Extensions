using System.Data;
using Dapper_Extensions.Crud.Enums;
using Dapper_Extensions.Crud.Extensions;
using Dapper_Extensions.Crud.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Dapper_Extensions.Crud.Tests.Extensions;

public class DapperCrudServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDapperCrud_WithSqlServer_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=password;";

        // Act
        // Note: This call registers a factory for IDbConnection that creates a new SqlConnection,
        // calls Open(), and registers IRepository<> and IUnitOfWork.
        services.AddDapperCrud(connectionString, DatabaseProvider.SqlServer);

        // Assert
        // Verify that the service collection contains an IDbConnection registration.
        services.Should().Contain(sd => sd.ServiceType == typeof(IDbConnection));
        // Verify that the repository and unit of work registrations exist.
        services.Should().Contain(sd => sd.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>));
    }

    [Fact]
    public void AddDapperCrud_WithPostgreSql_RegistersExpectedServices_AndAcceptsEnumMappings()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=TestDb;Username=test;Password=password;";
        var enumMappings = new Dictionary<Type, string>
        {
            { typeof(DummyEnum), "dummy" }
        };

        // Act
        services.AddDapperCrud(connectionString, DatabaseProvider.PostgreSql, enumMappings);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IDbConnection));
        services.Should().Contain(sd => sd.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(sd =>
            sd.ServiceType.IsGenericType &&
            sd.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>));
    }

    [Fact]
    public void AddDapperCrud_WithUnsupportedProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register a logger required by the connection factory.
        services.AddLogging();
        var connectionString = "AnyConnectionString";
        var unsupportedProvider = (DatabaseProvider)999;

        services.AddDapperCrud(connectionString, unsupportedProvider);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        Action act = () => serviceProvider.GetRequiredService<IDbConnection>();

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Unsupported provider");
    }

    // Dummy enum for testing enum mappings.
    private enum DummyEnum
    {
        Value1,
        Value2
    }
}