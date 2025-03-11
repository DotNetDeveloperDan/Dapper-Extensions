using System;
using System.Data;
using System.Collections.Generic;
using Dapper_Extensions.Crud;
using Dapper_Extensions.Crud.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Dapper_Extensions.Crud.Tests
{
    // Dummy entity for testing purposes.
    public class DummyEntity
    {
        public int Id { get; set; }
    }

    public class UnitOfWorkTests
    {
        [Fact]
        public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new UnitOfWork(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("connection");
        }

        [Fact]
        public void Repository_ShouldReturnSameInstance_ForSameEntityType()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            var uow = new UnitOfWork(mockConnection.Object);

            // Act
            var repo1 = uow.Repository<DummyEntity>();
            var repo2 = uow.Repository<DummyEntity>();

            // Assert
            repo1.Should().BeSameAs(repo2);
        }

        [Fact]
        public void BeginTransaction_ShouldOpenConnectionAndAssignTransactionToRepositories()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            // Simulate a closed connection.
            mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Closed);
            var dummyTransaction = new Mock<IDbTransaction>();
            // Setup BeginTransaction to return our dummy transaction.
            mockConnection.Setup(c => c.BeginTransaction()).Returns(dummyTransaction.Object);

            var uow = new UnitOfWork(mockConnection.Object);
            var repo = uow.Repository<DummyEntity>(); // Repository is created without transaction.

            // Act
            uow.BeginTransaction();

            // Assert
            mockConnection.Verify(c => c.Open(), Times.Once, "the connection should be opened when starting a transaction");
            // The repository should now have the transaction assigned if it implements ITransactionRepository.
            (repo as ITransactionRepository)?.Transaction.Should().Be(dummyTransaction.Object);
        }

        [Fact]
        public void Commit_ShouldThrowInvalidOperationException_WhenNoActiveTransaction()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            var uow = new UnitOfWork(mockConnection.Object);

            // Act
            Action act = () => uow.Commit();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No active transaction to commit.");
        }

        [Fact]
        public void Commit_ShouldCommitTransactionAndClearRepositoryTransactions()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            // Simulate an open connection.
            mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);
            var dummyTransaction = new Mock<IDbTransaction>();
            dummyTransaction.Setup(t => t.Commit()).Verifiable();
            dummyTransaction.Setup(t => t.Dispose()).Verifiable();
            mockConnection.Setup(c => c.BeginTransaction()).Returns(dummyTransaction.Object);

            var uow = new UnitOfWork(mockConnection.Object);
            var repo = uow.Repository<DummyEntity>();
            uow.BeginTransaction();
            // Pre-assert: repository's transaction should be set.
            (repo as ITransactionRepository)?.Transaction.Should().Be(dummyTransaction.Object);

            // Act
            uow.Commit();

            // Assert
            dummyTransaction.Verify(t => t.Commit(), Times.Once);
            dummyTransaction.Verify(t => t.Dispose(), Times.Once);
            (repo as ITransactionRepository)?.Transaction.Should().BeNull();
        }

        [Fact]
        public void Rollback_ShouldThrowInvalidOperationException_WhenNoActiveTransaction()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            var uow = new UnitOfWork(mockConnection.Object);

            // Act
            Action act = () => uow.Rollback();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No active transaction to roll back.");
        }

        [Fact]
        public void Rollback_ShouldRollbackTransactionAndClearRepositoryTransactions()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);
            var dummyTransaction = new Mock<IDbTransaction>();
            dummyTransaction.Setup(t => t.Rollback()).Verifiable();
            dummyTransaction.Setup(t => t.Dispose()).Verifiable();
            mockConnection.Setup(c => c.BeginTransaction()).Returns(dummyTransaction.Object);

            var uow = new UnitOfWork(mockConnection.Object);
            var repo = uow.Repository<DummyEntity>();
            uow.BeginTransaction();

            // Act
            uow.Rollback();

            // Assert
            dummyTransaction.Verify(t => t.Rollback(), Times.Once);
            dummyTransaction.Verify(t => t.Dispose(), Times.Once);
            (repo as ITransactionRepository)?.Transaction.Should().BeNull();
        }

        [Fact]
        public void Dispose_ShouldDisposeTransactionAndConnection_WhenTransactionActive()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            mockConnection.SetupGet(c => c.State).Returns(ConnectionState.Open);
            var dummyTransaction = new Mock<IDbTransaction>();
            mockConnection.Setup(c => c.BeginTransaction()).Returns(dummyTransaction.Object);

            var uow = new UnitOfWork(mockConnection.Object);
            uow.BeginTransaction();

            // Act
            uow.Dispose();

            // Assert
            dummyTransaction.Verify(t => t.Dispose(), Times.Once);
            mockConnection.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_ShouldDisposeConnection_WhenNoTransactionActive()
        {
            // Arrange
            var mockConnection = new Mock<IDbConnection>();
            var uow = new UnitOfWork(mockConnection.Object);

            // Act
            uow.Dispose();

            // Assert
            mockConnection.Verify(c => c.Dispose(), Times.Once);
        }
    }
}
