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
    public class TestDbContextFactory<TDbContext> : IDisposable
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
        /// <returns>A new instance of the <typeparamref name="TDbContext"/> in initial state.</returns>
        /// <exception cref="ArgumentNullException">connection is null.</exception>
        /// <exception cref="TestDbContextFactoryException">
        /// The constructor of the <typeparamref name="TDbContext"/> doesn't
        /// have a single parameter with type <see cref="DbContextOptions{TDbContext}"/>;
        /// </exception>
        public TDbContext CreateDbContext()
        {
            var instanceConnection = CreateDbConnection();
            return CreateDbContext(instanceConnection);
        }

        /// <summary>
        /// Creates an instance from <typeparamref name="TDbContext"/> inicializing
        /// with the provided SqliteConnection.
        /// </summary>
        /// <remarks>
        /// This call doesn't initializes the database but
        /// intended to use to create a new DbContext on the same connection.
        /// </remarks>
        /// <param name="connection">The connection to use for the DbContext.</param>
        /// <returns>A new instace of <typeparamref name="TDbContext"/>
        /// with the provided connection as underlying database.</returns>
        /// <exception cref="ArgumentNullException">connection is null.</exception>
        /// <exception cref="TestDbContextFactoryException">
        /// The constructor of the <typeparamref name="TDbContext"/> doesn't
        /// have a single parameter with type <see cref="DbContextOptions{TDbContext}"/>;
        /// </exception>
        public TDbContext CreateDbContext(SqliteConnection connection)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var options = new DbContextOptionsBuilder<TDbContext>();
            options.UseSqlite(connection);
            ConfigureDbContextOptionsBuilder(options, false);

            return _constructor.Value(options.Options);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SqliteConnection"/> and
        /// copies the data from the prototype connection. On first call this creates
        /// the prototype connection.
        /// </summary>
        /// <returns>A new instance of the <see cref="SqliteConnection"/> in initial state.</returns>
        public SqliteConnection CreateDbConnection()
        {
            LazyInitializer.EnsureInitialized(
                   ref _prototypeConnection,
                   ref _initialized,
                   ref _syncLock,
                   CreatePrototypeConnection);
            var instanceConnection = new SqliteConnection(InstanceConnectionString);
            instanceConnection.Open();
            _prototypeConnection.BackupDatabase(instanceConnection);
            return instanceConnection;
        }

        private SqliteConnection CreatePrototypeConnection()
        {
            var prototypeConnection = new SqliteConnection(PrototypeConnectionString);
            if (ShouldRunDatabasePreparation(prototypeConnection))
            {
                prototypeConnection.Open();
                using (var dbContext = CreatePrototypeDbContextInstance(prototypeConnection))
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
                throw new TestDbContextFactoryException($"Either CreateDbContextInstance must be overidden or {typeof(DbContextOptions).Name} needs a constructor with DbContextOptions parameter.");
            var parameter = Expression.Parameter(typeof(DbContextOptions<TDbContext>), "options");
            var ctor = Expression.New(constructor, parameter);
            var lambda = Expression.Lambda<Func<DbContextOptions<TDbContext>, TDbContext>>(ctor, parameter);
            return lambda.Compile();
        }

        private TDbContext CreatePrototypeDbContextInstance(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<TDbContext>();
            options.UseSqlite(connection);
            ConfigureDbContextOptionsBuilder(options, true);

            return _constructor.Value(options.Options);
        }

        /// <summary>
        /// Adds additional configurations to the <see cref="DbContextOptionsBuilder{TDbContext}"/>
        /// in the derived classes.
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
