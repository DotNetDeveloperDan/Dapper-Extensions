using Microsoft.Data.SqlClient;
using Npgsql;

namespace Dapper_Extensions.Crud.Tests;

public class DbConnectionFactoryTests
{
    [Fact]
    public void CreateConnection_ShouldReturnSqlConnection_WhenConnectionStringContainsServer()
    {
        // Arrange
        var connectionString = "Server=localhost;Database=TestDb;User Id=sa;Password=password;";
        var factory = new DbConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().BeOfType<SqlConnection>()
            .Which.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void CreateConnection_ShouldReturnSqlConnection_WhenConnectionStringContainsDataSource()
    {
        // Arrange
        var connectionString = "Data Source=localhost;Initial Catalog=TestDb;User ID=sa;Password=password;";
        var factory = new DbConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().BeOfType<SqlConnection>()
            .Which.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void CreateConnection_ShouldReturnNpgsqlConnection_WhenConnectionStringContainsHost()
    {
        // Arrange
        var connectionString = "Host=localhost;Database=TestDb;Username=test;Password=password;";
        var factory = new DbConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().BeOfType<NpgsqlConnection>()
            .Which.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void Constructor_ShouldThrowNotSupportedException_WhenConnectionStringIsInvalid()
    {
        // Arrange
        var connectionString = "InvalidConnectionStringWithoutProviderInfo";

        // Act
        Action act = () => new DbConnectionFactory(connectionString);

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Unable to determine database provider from the connection string.");
    }

    [Fact]
    public void CreateConnection_ShouldBeCaseInsensitive_ForSqlServerKeywords()
    {
        // Arrange
        var connectionString = "server=localhost;Database=TestDb;User Id=sa;Password=password;";
        var factory = new DbConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().BeOfType<SqlConnection>();
    }

    [Fact]
    public void CreateConnection_ShouldBeCaseInsensitive_ForPostgreSqlKeywords()
    {
        // Arrange
        var connectionString = "host=localhost;Database=TestDb;Username=test;Password=password;";
        var factory = new DbConnectionFactory(connectionString);

        // Act
        var connection = factory.CreateConnection();

        // Assert
        connection.Should().BeOfType<NpgsqlConnection>();
    }
}