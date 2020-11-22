# Foxy.Testing.EntityFrameworkCore
A library that improves SqlLite based Entity Framework Core test runs speed.

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

Your DbContext must have a constructor with ```DbContextOptions``` parameter.
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

And lastly use in your test. (Example with xUnit)
```csharp
    [Fact]
    public void TestMyDb() {
        using var dbContext = TestDatabase.Instance.CreateDbContext();

        // Perform your test.
    }
```

Or create a DbConnection and use it directly. You can create a DbContext easly with CreateDbContext(connection). This helps if multiple DbContext needed with the same connection.
```csharp
    [Fact]
    public void TestMyDb() {
        using var connection = TestDatabase.Instance.CreateDbConnection();
        // Database is prepared here. The next line just creates a DbContext.
        using var dbConext = TestDatabase.Instance.CreateDbContext(connection);
        // Perform your test.
    }
```

# Configuration and extension points
## Constructor parameters
There are two connection needed by ```BaseTestDatabaseFactory``` which can be provided in the constructor parameters. the default wor both is ```"Data Source=:memory:;"```
 - prototypeConnectionString: This is used on the first run. It will be the connectionstring for the database which are held on the entire test run and copyed from.
 - instanceConnectionString: This is used in every CreateDbContext. It is fillled with data from the prototype connection.

If you would like to speed up the prototype database creation also you can change the prototypeConnectionString to point to a disk file. If the file was already created then the database preparation skipped otherwise it is performed.

## PrepareDbContext
In this overridable class you can fill the prototype database with data. This is called only once. On the first initialization.

## ShouldRunDatabasePreparation
Determines if the migration and the db context preparation should be executed. This method is overridable. By default, it returns true if the database in memory or if a non existing disk file.

## ExecuteMigrate
Executes the migration. It is overridable. Unfortunatelly not every migration step is performable on sqlite database so I left this open to change.

## ConfigureDbContextOptionsBuilder
Overridable method in which you can add additional setups to the ```DbContextOptionsBuilder```. For both prototype and instace connection. the ```isPrototype``` parameter determines which is created.

# Benchmark
Here is some benchmark about the database creation speed. The ClassicCreation creates a sqlite database and fills it with data from csv-s. In the FastCreation the prototype database is in memory. In the SnapshotCreation the prepared prototype database loaded from disk.
The FastCreation helps best when the initialization is small. The SnapshotCreation helps best when the initialization is big like when you fill up the Northwind database with the example data.

<pre>
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
AMD Ryzen 5 2600, 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  Job-UAGRXG : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
</pre>
<pre>OutlierMode=RemoveAll</pre>
|Method|Mean|Error|StdDev|Rank|
|--- |--- |--- |--- |--- |
|ClassicCreation|713,003.6 μs|5,133.79 μs|4,550.97 μs|3|
|FastCreation|352.7 μs|6.64 μs|7.65 μs|1|
|SnapshotCreation|1,873.1 μs|28.68 μs|47.93 μs|2|
