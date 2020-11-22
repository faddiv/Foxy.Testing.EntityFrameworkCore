using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NorthwindDatabase;
using System.Data;
using Xunit;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class DbConnectionFactoryTest
    {
        [Fact]
        public void CreateDbConnection_creates_an_instance()
        {
            using var factory = new NorthWindDatabaseFactory();

            using var dbConnection = factory.CreateDbConnection();

            // Assert
            dbConnection.Should().NotBeNull();
        }

        [Fact]
        public void CreateDbConnection_opens_the_database()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            using var connection = instance.CreateDbConnection();

            // Assert
            connection.State.Should().Be(ConnectionState.Open);
        }

        [Fact]
        public void CreateDbConnection_prepares_the_db_context()
        {
            using var factory = new NorthWindDatabaseFactory();

            using var dbConnection = factory.CreateDbConnection();

            TestHelpers.ShouldBePrepared(dbConnection);
        }

        [Fact]
        public void CreateDbConnection_calls_prepare_only_once()
        {
            // Arrange
            var count = 0;
            using var instance = new NorthWindDatabaseFactory();
            instance.Prepared += () => count++;

            // Act
            TestHelpers.MakeFirstCall(instance);
            using var dbConnection = instance.CreateDbConnection();

            // Assert
            count.Should().Be(1);
            TestHelpers.ShouldBePrepared(dbConnection);
        }

    }
}
