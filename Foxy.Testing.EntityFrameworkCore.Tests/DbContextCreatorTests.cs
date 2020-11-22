using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class DbContextCreatorTests
    {
        [Fact]
        public void CreateDbContext_connection_inicializes_a_DbConext_with_given_connection()
        {
            using var factory = new NorthwindDatabase.NorthWindDatabaseFactory();
            using var connection = factory.CreateDbConnection();

            var dbContext = factory.CreateDbContext(connection);

            var actual = dbContext.Database.GetDbConnection();
            actual.Should().BeSameAs(connection);
        }

        [Fact]
        public void CreateDbContext_connection_calls_ConfigureDbContextOptionsBuilder()
        {
            using var factory = new NorthwindDatabase.NorthWindDatabaseFactory();
            using var connection = factory.CreateDbConnection();

            var dbContext = factory.CreateDbContext(connection);

            factory.LastIsPrototype.Should().BeFalse();
            factory.Builder.Should().NotBeNull();
        }
    }
}
