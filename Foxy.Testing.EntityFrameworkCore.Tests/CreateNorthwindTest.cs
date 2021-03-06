using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NorthwindDatabase;
using Xunit;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class CreateNorthwindTest
    {
        [Fact]
        public void DatabaseCreated()
        {
            var scaffold = new DatabaseScaffold();
            using var connection = new SqliteConnection("Data Source=:memory:;");
            connection.Open();
            using (var context = TestDbContextProvider.CreateDbContext(connection))
            {
                context.Database.Migrate();
                scaffold.Run(context);
            }
            using (var context = TestDbContextProvider.CreateDbContext(connection))
            {
                TestHelpers.ShouldBePrepared(context);
            }
        }
    }
}
