using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;

namespace Foxy.Testing.EntityFrameworkCore
{
    /// <summary>
    /// Provides base class for test DbContext factory classes (or can be used by itself if no setup is needed).
    /// This class mades efficient creating DbContext with predictable initial state in SQLite database.
    /// </summary>
    /// <typeparam name="TDbContext">The derived class of DbContext to test.</typeparam>
    public class TestDbContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        private SqliteConnection _prototypeConnection;
        private object _syncLock = new object();
        private bool _initialized;
        private bool _disposedValue = false; // To detect redundant calls
        private readonly Lazy<Func<DbContextOptions<TDbContext>, TDbContext>> _constructor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextFactory{TDbContext}"/> class.
        /// </summary>
        /// <param name="prototypeConnectionString">Connection string used for the prototype connection. Default is memory database.</param>
        /// <param name="instanceConnectionString">Connection string used for the test database instances. Default is memory database.</param>
        public TestDbContextFactory(
            string prototypeConnectionString = "Data Source=:memory:;",
            string instanceConnectionString = "Data Source=:memory:;")
        {
            PrototypeConnectionString = prototypeConnectionString;
            InstanceConnectionString = instanceConnectionString;
            _constructor = new Lazy<Func<DbContextOptions<TDbContext>, TDbContext>>(
                CreateDbContextFactory, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the connection string used for the prototype connection.
        /// </summary>
        public string PrototypeConnectionString { get; }

        /// <summary>
        /// Gets the connection string used for the test database instances.
        /// </summary>
        public string InstanceConnectionString { get; }

        /// <summary>
        /// Creates a new instance of the <typeparamref name="TDbContext"/> and
        /// copies the data from the prototype connection. On first call this creates
        /// the prototype connection.
        /// </summary>
        /// <returns>A new instance of <typeparamref name="TDbContext"/> in initial state.</returns>
        public TDbContext CreateDbContext()
        {
            LazyInitializer.EnsureInitialized(
                ref _prototypeConnection,
                ref _initialized,
                ref _syncLock,
                CreatePrototypeConnection);
            var instanceConnection = new SqliteConnection(InstanceConnectionString);
            instanceConnection.Open();
            _prototypeConnection.BackupDatabase(instanceConnection);
            return CreateDbContextInstance(instanceConnection, false);
        }

        private SqliteConnection CreatePrototypeConnection()
        {
            var prototypeConnection = new SqliteConnection(PrototypeConnectionString);
            if (ShouldRunDatabasePreparation(prototypeConnection))
            {
                prototypeConnection.Open();
                using (var dbContext = CreateDbContextInstance(prototypeConnection, true))
                {
                    ExecuteMigrate(dbContext);
                    PrepareDbContext(dbContext);
                    dbContext.SaveChanges();
                }
            }
            else
            {
                prototypeConnection.Open();
            }
            return prototypeConnection;
        }

        /// <summary>
        /// Calls the <see cref="RelationalDatabaseFacadeExtensions.Migrate(Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade)"/> method.
        /// </summary>
        /// <param name="dbContext">The <typeparamref name="TDbContext"/> on which the migration is called.</param>
        protected virtual void ExecuteMigrate(TDbContext dbContext)
        {
            dbContext.Database.Migrate();
        }

        /// <summary>
        /// Determines if the migration and the db context preparation should be executed.
        /// </summary>
        /// <param name="prototypeConnection">The unopened <see cref="SqliteConnection"/> which will be used as prototype connection.</param>
        /// <returns>True if the connection is in memory or points to an existing disk file.</returns>
        protected virtual bool ShouldRunDatabasePreparation(SqliteConnection prototypeConnection)
        {
            return string.Equals(prototypeConnection.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(prototypeConnection.DataSource);
        }

        private static Func<DbContextOptions<TDbContext>, TDbContext> CreateDbContextFactory()
        {
            Type dbContextType = typeof(TDbContext);
            var constructor = dbContextType.GetConstructor(new[] { typeof(DbContextOptions) });
            if(constructor == null)
            {
                constructor = dbContextType.GetConstructor(new[] { typeof(DbContextOptions<TDbContext>) });
            }
            if (constructor == null)
                throw new Exception($"Either CreateDbContextInstance must be overidden or {typeof(DbContextOptions).Name} needs a constructor with DbContextOptions parameter.");
            var parameter = Expression.Parameter(typeof(DbContextOptions<TDbContext>), "options");
            var ctor = Expression.New(constructor, parameter);
            var lambda = Expression.Lambda<Func<DbContextOptions<TDbContext>, TDbContext>>(ctor, parameter);
            return lambda.Compile();
        }

        /// <summary>
        /// Calls the constructor of the <typeparamref name="TDbContext"/> with a
        /// <see cref="DbContextOptionsBuilder{TDbContext}"/> prepared for sqlite usage.
        /// </summary>
        /// <param name="connection">The connection used for the <typeparamref name="TDbContext"/>.</param>
        /// <param name="isPrototype">If true then the prototype DbContext is created.</param>
        /// <returns>A new instance of <see cref="DbContextOptionsBuilder{TDbContext}"/>.</returns>
        protected virtual TDbContext CreateDbContextInstance(SqliteConnection connection, bool isPrototype)
        {
            var options = new DbContextOptionsBuilder<TDbContext>();
            options.UseSqlite(connection);
            ConfigureDbContextOptionsBuilder(options, isPrototype);

            return _constructor.Value(options.Options);
        }

        /// <summary>
        /// Adds additional configurations in the derived classes.
        /// </summary>
        /// <param name="builder">The builder to configure.</param>
        /// <param name="isPrototype">If true then the prototype DbContext is created.</param>
        protected virtual void ConfigureDbContextOptionsBuilder(DbContextOptionsBuilder<TDbContext> builder, bool isPrototype)
        {

        }

        /// <summary>
        /// In the derived classes adds the initial data to the database. The SaveChanges is called after.
        /// </summary>
        /// <param name="context">The db context to use in database initialization.</param>
        protected virtual void PrepareDbContext(TDbContext context)
        {
        }

        #region IDisposable Support

        /// <summary>
        /// On true disposes the prototype connection.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _prototypeConnection?.Dispose();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes the prototype connection.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
