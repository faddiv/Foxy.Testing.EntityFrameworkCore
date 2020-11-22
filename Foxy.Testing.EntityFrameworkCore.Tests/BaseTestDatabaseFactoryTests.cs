using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NorthwindDatabase;
using System.Data;
using Xunit;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class BaseTestDatabaseFactoryTests
    {
        [Fact]
        public void Constructor_uses_default_parameters()
        {
            // Arrange
            // Act
            using var instance = new NorthWindDatabaseFactory();

            // Assert
            instance.PrototypeConnectionString.Should().Be("Data Source=:memory:;");
            instance.InstanceConnectionString.Should().Be("Data Source=:memory:;");
        }

        [Fact]
        public void CreateDbContext_creates_an_instance()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            using var dbContext = instance.CreateDbContext();

            // Assert
            dbContext.Should().NotBeNull();
        }

        [Fact]
        public void CreateDbContext_opens_the_database()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            using var dbContext = instance.CreateDbContext();

            // Assert
            var connection = dbContext.Database.GetDbConnection();
            connection.State.Should().Be(ConnectionState.Open);
        }

        [Fact]
        public void CreateDbContext_prepares_the_db_context()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            using var dbContext = instance.CreateDbContext();

            // Assert
            dbContext.Should().NotBeNull();
            TestHelpers.ShouldBePrepared(dbContext);
        }

        [Fact]
        public void CreateDbContext_calls_prepare_only_once()
        {
            // Arrange
            var count = 0;
            using var instance = new NorthWindDatabaseFactory();
            instance.Prepared += () => count++;

            // Act
            TestHelpers.MakeFirstCall(instance);
            using var dbContext = instance.CreateDbContext();

            // Assert
            count.Should().Be(1);
            TestHelpers.ShouldBePrepared(dbContext);
        }

        [Fact]
        public void Constructor_can_work_snapshot_mode()
        {
            // Arrange
            var count = 0;
            using var instance = new NorthWindDatabaseFactory(snapshot: true);
            instance.Prepared += () => count++;

            // Act
            TestHelpers.MakeFirstCall(instance);
            using var dbContext = instance.CreateDbContext();

            // Assert
            count.Should().Be(0);
            TestHelpers.ShouldBePrepared(dbContext);
        }

        [Fact]
        public void ConfigureDbContextOptionsBuilder_is_called()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            TestHelpers.MakeFirstCall(instance);

            // Assert
            instance.Builder.Should().NotBeNull();
        }

        [Fact]
        public void ExecuteMigrate_is_called()
        {
            // Arrange
            using var instance = new NorthWindDatabaseFactory();

            // Act
            using var dbContext = instance.CreateDbContext();

            // Assert
            instance.MigratedDbContext.Should().NotBeNull();
            instance.MigratedDbContext.Should().NotBeSameAs(dbContext);
        }

        [Fact]
        public void Works_with_generic_DbContextOptions()
        {
            using var factory = new OtherDbContextFactory();

            using var result = factory.CreateDbContext();

            result.Should().NotBeNull();
        }

    }
}
