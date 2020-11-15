# Foxy.Testing.EntityFrameworkCore
A library that helps speeding up SqlLite based Entity Framework Core tests.

There are two approach in the microsoft's entity framework core documentation for testing database queries. One is [test with InMemory](https://docs.microsoft.com/en-us/ef/core/testing/in-memory) and the other is [test with SQLite](https://docs.microsoft.com/en-us/ef/core/testing/sqlite). Both has their benefits and drawbacks. I prefer the SQLite approach. One of the drawbacks is that it can be very slow if there is lots of db unit tests even with in memory database. The reason is to use it in an unit test in isolation is that you need to set up an initial database for every test. You can boost this setup greatly if you init a database once then you use a copy of it which can be created with ```SqliteConnection.BackupDatabase``` method. This package does exactly that and hides it behind a nice facade.

# Basic usage
The simplest form you just need to create a derived class from the ```BaseTestDatabaseFactory<YourDbContext>``` and optionally prepare the initial data.

```csharp
public class YourDatabaseFactory : BaseTestDatabaseFactory<YourDbContext>
{
    protected override void PrepareDbContext(TestDbContext context)
    {
        // Initialize your database with data
    }
}
```

And your DbContext must have a constructor with ```DbContextOptions``` parameter.
```csharp
    public YourDbContext(DbContextOptions options) : base(options) { }
```
Or
```csharp
    public YourDbContext(DbContextOptions<YourDbContext> options) : base(options) { }
```

Then you make an instance into a static field or Property.
```csharp
    public static class TestDatabase {
        public static YourDatabaseFactory Instance { get; } = new YourDatabaseFactory(
    }
```

And lastly use in your test. (Example wit xUnit)
```csharp
    [Fact]
    public void TestMyDb() {
        using var dbContext = TestDatabase.Instance.CreateDbContext();

        // Perform your test.
    }
```

# Additional use cases
## Constructor parameters
There are two connection needed by ```BaseTestDatabaseFactory``` which can be provided in the constructor parameters. the default wor both is ```"Data Source=:memory:;"```
 - prototypeConnectionString: This is used on the first run. It will be the connectionstring for the database which are held on the entire test run and copyed from.
 - instanceConnectionString: This is used in every CreateDbContext. It is fillled with data from the prototype connection.

If you would like to speed up the prototype database creation also you can change the prototypeConnectionString to point to a disk file. If the file was already created then the database preparation skipped otherwise it is performed.

## ShouldRunDatabasePreparation
TODO

## Migration
TODO

## Prepare DbContextOptionsBuilder
TODO

