using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Dapper_Extensions.Crud;
using Dapper_Extensions.Crud.Interfaces;
using Dapper_Extensions.Crud.Interfaces.DapperCrudLibrary.Mapping;
using Microsoft.Data.SqlClient;
using Npgsql;
using Moq;
using Xunit;
using FluentAssertions;
using Dapper_Extensions.Crud.Enums;

namespace DapperExtensions.Tests
{
    public class RepositoryTests
    {
        // Dummy entity for single key operations.
        public class DummyEntity
        {
            [Key]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // Dummy entity for composite key operations.
        [Table("DummyComposite")]
        public class DummyCompositeEntity
        {
            [Key]
            public int Id1 { get; set; }
            [Key]
            public int Id2 { get; set; }
            public string Description { get; set; }
        }

        // Dummy mapping used by batch upsert tests.
        public class DummyEntityMapping : IEntityMapping<DummyEntity>
        {
            public string TableName => "DummyEntityTable";
            public IEnumerable<string> KeyProperties => new List<string> { "Id" };
        }

        // Helper to create a mock connection.
        private Mock<IDbConnection> CreateMockConnection()
        {
            return new Mock<IDbConnection>();
        }

        #region CRUD Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsEntity()
        {
            // Arrange
            var expected = new DummyEntity { Id = 1, Name = "Test" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.GetAsync<DummyEntity>(
                    mockConnection.Object,
                    1,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetById_ReturnsEntity()
        {
            // Arrange
            var expected = new DummyEntity { Id = 1, Name = "Test" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Get<DummyEntity>(
                    mockConnection.Object,
                    1,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.GetById(1);

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEntities()
        {
            // Arrange
            var expected = new List<DummyEntity>
            {
                new DummyEntity { Id = 1, Name = "Test1" },
                new DummyEntity { Id = 2, Name = "Test2" }
            };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.GetAllAsync<DummyEntity>(
                    mockConnection.Object,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetAll_ReturnsEntities()
        {
            // Arrange
            var expected = new List<DummyEntity>
            {
                new DummyEntity { Id = 1, Name = "Test1" },
                new DummyEntity { Id = 2, Name = "Test2" }
            };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.GetAll<DummyEntity>(
                    mockConnection.Object,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.GetAll();

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task AddAsync_ReturnsNewId()
        {
            // Arrange
            var entity = new DummyEntity { Id = 0, Name = "New" };
            var newId = 100L;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.InsertAsync<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(newId);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.AddAsync(entity);

            // Assert
            result.Should().Be(newId);
        }

        [Fact]
        public void Add_ReturnsNewId()
        {
            // Arrange
            var entity = new DummyEntity { Id = 0, Name = "New" };
            var newId = 100L;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Insert<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(newId);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.Add(entity);

            // Assert
            result.Should().Be(newId);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue()
        {
            // Arrange
            var entity = new DummyEntity { Id = 1, Name = "Update" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.UpdateAsync<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.UpdateAsync(entity);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Update_ReturnsTrue()
        {
            // Arrange
            var entity = new DummyEntity { Id = 1, Name = "Update" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Update<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.Update(entity);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue()
        {
            // Arrange
            var entity = new DummyEntity { Id = 1, Name = "Delete" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.DeleteAsync<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.DeleteAsync(entity);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Delete_ReturnsTrue()
        {
            // Arrange
            var entity = new DummyEntity { Id = 1, Name = "Delete" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Delete<DummyEntity>(
                    mockConnection.Object,
                    entity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.Delete(entity);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Stored Procedure Tests

        [Fact]
        public async Task ExecuteStoredProcedureAsync_ReturnsData()
        {
            // Arrange
            var storedProcName = "sp_Test";
            var parameters = new { Param = 1 };
            var expected = new List<string> { "result1", "result2" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.QueryAsync<string>(
                    mockConnection.Object,
                    storedProcName,
                    parameters,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>(),
                    CommandType.StoredProcedure))
                .ReturnsAsync(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.ExecuteStoredProcedureAsync<string>(storedProcName, parameters);

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ExecuteStoredProcedure_ReturnsData()
        {
            // Arrange
            var storedProcName = "sp_Test";
            var parameters = new { Param = 1 };
            var expected = new List<string> { "result1", "result2" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Query<string>(
                    mockConnection.Object,
                    storedProcName,
                    parameters,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    CommandType.StoredProcedure))
                .Returns(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.ExecuteStoredProcedure<string>(storedProcName, parameters);

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task ExecuteStoredProcedureAsync_NonQuery_ReturnsInt()
        {
            // Arrange
            var storedProcName = "sp_NonQuery";
            var parameters = new { Param = 1 };
            var expected = 5;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.ExecuteAsync(
                    mockConnection.Object,
                    storedProcName,
                    parameters,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>(),
                    CommandType.StoredProcedure))
                .ReturnsAsync(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.ExecuteStoredProcedureAsync(storedProcName, parameters);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExecuteStoredProcedure_NonQuery_ReturnsInt()
        {
            // Arrange
            var storedProcName = "sp_NonQuery";
            var parameters = new { Param = 1 };
            var expected = 5;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();
            mockExecutor.Setup(x => x.Execute(
                    mockConnection.Object,
                    storedProcName,
                    parameters,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>(),
                    CommandType.StoredProcedure))
                .Returns(expected);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.ExecuteStoredProcedure(storedProcName, parameters);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region Upsert and Batch Upsert Tests

        [Fact]
        public async Task UpsertAsync_SingleKey_NewEntity_CallsAddAsync()
        {
            // Arrange: new entity (default key) so GetAsync returns null.
            var newEntity = new DummyEntity { Id = 0, Name = "New" };
            var newId = 200L;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.GetAsync<DummyEntity>(
                    mockConnection.Object,
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync((DummyEntity)null);

            mockExecutor.Setup(x => x.InsertAsync<DummyEntity>(
                    mockConnection.Object,
                    newEntity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(newId);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.UpsertAsync(newEntity);

            // Assert
            result.Should().Be(newId);
        }

        [Fact]
        public async Task UpsertAsync_SingleKey_ExistingEntity_CallsUpdateAsync()
        {
            // Arrange: existing entity so GetAsync returns a non-null value.
            var existingEntity = new DummyEntity { Id = 1, Name = "Existing" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.GetAsync<DummyEntity>(
                    mockConnection.Object,
                    existingEntity.Id,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(existingEntity);
            mockExecutor.Setup(x => x.UpdateAsync<DummyEntity>(
                    mockConnection.Object,
                    existingEntity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = await repository.UpsertAsync(existingEntity);

            // Assert – the returned value should be the existing entity’s Id.
            result.Should().Be(existingEntity.Id);
        }

        [Fact]
        public void Upsert_SingleKey_NewEntity_CallsAdd()
        {
            // Arrange: new entity (default key) so Get returns null.
            var newEntity = new DummyEntity { Id = 0, Name = "New" };
            var newId = 300L;
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.Get<DummyEntity>(
                    mockConnection.Object,
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns((DummyEntity)null);
            mockExecutor.Setup(x => x.Insert<DummyEntity>(
                    mockConnection.Object,
                    newEntity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(newId);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.Upsert(newEntity);

            // Assert
            result.Should().Be(newId);
        }

        [Fact]
        public void Upsert_SingleKey_ExistingEntity_CallsUpdate()
        {
            // Arrange: existing entity so Get returns a non-null value.
            var existingEntity = new DummyEntity { Id = 1, Name = "Existing" };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.Get<DummyEntity>(
                    mockConnection.Object,
                    existingEntity.Id,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(existingEntity);
            mockExecutor.Setup(x => x.Update<DummyEntity>(
                    mockConnection.Object,
                    existingEntity,
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>()))
                .Returns(true);

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object);

            // Act
            var result = repository.Upsert(existingEntity);

            // Assert
            result.Should().Be(existingEntity.Id);
        }

        [Fact]
        public async Task UpsertListBatchAsync_ReturnsCounts()
        {
            // Arrange:
            EntityMappingRegistry.Register(new DummyEntityMapping());
            var entities = new List<DummyEntity>
    {
        new DummyEntity { Id = 0, Name = "Entity1" },
        new DummyEntity { Id = 0, Name = "Entity2" }
    };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.QueryAsync<string>(
                    mockConnection.Object,
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<int?>(),
                    CommandType.Text))
                .ReturnsAsync(new List<string> { "INSERT", "UPDATE" });

            // Pass a supported provider (SqlServer) so the batch upsert branch is used.
            // Set batchSize to 2 so that both entities are processed in a single batch.
            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object, DatabaseProvider.SqlServer);

            // Act
            var (inserted, updated) = await repository.UpsertListBatchAsync(entities, batchSize: 2);

            // Assert
            inserted.Should().Be(1);
            updated.Should().Be(1);
        }

        [Fact]
        public void UpsertListBatch_ReturnsCounts()
        {
            // Arrange:
            EntityMappingRegistry.Register(new DummyEntityMapping());
            var entities = new List<DummyEntity>
    {
        new DummyEntity { Id = 0, Name = "Entity1" },
        new DummyEntity { Id = 0, Name = "Entity2" }
    };
            var mockConnection = CreateMockConnection();
            var mockExecutor = new Mock<IDapperExecutor>();

            mockExecutor.Setup(x => x.Query<string>(
                    mockConnection.Object,
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    CommandType.Text))
                .Returns(new List<string> { "INSERT", "UPDATE" });

            var repository = new Repository<DummyEntity>(mockConnection.Object, mockExecutor.Object, DatabaseProvider.SqlServer);

            // Act
            var (inserted, updated) = repository.UpsertListBatch(entities, batchSize: 2);

            // Assert
            inserted.Should().Be(1);
            updated.Should().Be(1);
        }



        #endregion
    }
}
