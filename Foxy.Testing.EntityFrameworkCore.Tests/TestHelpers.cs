using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NorthwindDatabase;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class TestHelpers
    {
        public static void ShouldBePrepared(SqliteConnection dbConnection)
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>();
            builder.UseSqlite(dbConnection);
            using var context = new TestDbContext(builder.Options);
            ShouldBePrepared(context);
        }
        public static void ShouldBePrepared(TestDbContext context)
        {
            context.Categories.Should().NotBeEmpty();
            context.Customers.Should().NotBeEmpty();
            context.Employees.Should().NotBeEmpty();
            context.EmployeeTerritories.Should().NotBeEmpty();
            context.OrderDetails.Should().NotBeEmpty();
            context.Orders.Should().NotBeEmpty();
            context.Products.Should().NotBeEmpty();
            context.Region.Should().NotBeEmpty();
            context.Shippers.Should().NotBeEmpty();
            context.Suppliers.Should().NotBeEmpty();
            context.Territories.Should().NotBeEmpty();
        }

        public static void MakeFirstCall(NorthWindDatabaseFactory instance)
        {
#pragma warning disable CS0642 // Possible mistaken empty statement
            using (instance.CreateDbContext()) ;
#pragma warning restore CS0642 // Possible mistaken empty statement
        }

    }
}
