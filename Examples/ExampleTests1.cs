using FluentAssertions;
using Foxy.Testing.EntityFrameworkCore;
using NorthwindDatabase;
using System;
using Xunit;

namespace Examples
{
    public class ExampleTests1
    {
        public readonly static TestDbContextFactory<TestDbContext> dbContextFactory
            = new TestDbContextFactory<TestDbContext>();

        [Fact]
        public void DbStartsEmpty()
        {
            using var db = dbContextFactory.CreateDbContext();

            db.Orders.Should().BeEmpty();
        }

        [Fact]
        public void Adding_Element_Saved_Into_Db()
        {
            var id = Guid.NewGuid().ToString();
            using var db = dbContextFactory.CreateDbConnection() ;
            using(var context = dbContextFactory.CreateDbContext(db))
            {
                context.Customers.Add(new Customers
                {
                    Address = "Address",
                    City = "City",
                    CompanyName = "CN",
                    ContactName = "CN",
                    ContactTitle = "CT",
                    Country = "Country",
                    CustomerId = id,
                    Fax = "FX",
                    Phone ="P",
                    PostalCode = "PC",
                    Region = "R"
                });
                context.SaveChanges();
            }

            using (var context = dbContextFactory.CreateDbContext(db))
            {
                var customer = context.Customers.Find(id);

                customer.Should().NotBeNull();
                customer.Address.Should().Be("Address");
                // ...
            }
        }
    }
}
