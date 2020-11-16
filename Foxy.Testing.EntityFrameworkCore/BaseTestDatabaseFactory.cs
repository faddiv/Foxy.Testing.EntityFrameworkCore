using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Threading;

namespace Foxy.Testing.EntityFrameworkCore
{
    public class BaseTestDatabaseFactory<TDbContext>
        where TDbContext : DbContext
    {
        private SqliteConnection _prototypeConnection;
        private object _syncLock = new object();
        private bool _initialized;
        private bool _disposedValue = false; // To detect redundant calls
        private Lazy<Func<DbContextOptions, TDbContext>> _constructor;

        public BaseTestDatabaseFactory(
            string prototypeConnectionString = "Data Source=:memory:;",
            string instanceConnectionString = "Data Source=:memory:;")
        {
            PrototypeConnectionString = prototypeConnectionString;
            InstanceConnectionString = instanceConnectionString;
            _constructor = new Lazy<Func<DbContextOptions, TDbContext>>(
                CreateDbContextFactory, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public string PrototypeConnectionString { get; }

        public string InstanceConnectionString { get; }

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

        protected virtual void ExecuteMigrate(TDbContext dbContext)
        {
            dbContext.Database.Migrate();
        }

        protected virtual bool ShouldRunDatabasePreparation(SqliteConnection prototypeConnection)
        {
            return string.Equals(prototypeConnection.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(prototypeConnection.DataSource);
        }

        private static Func<DbContextOptions, TDbContext> CreateDbContextFactory()
        {
            var constructor = typeof(TDbContext).GetConstructor(new[] { typeof(DbContextOptions) });
            if (constructor == null)
                throw new Exception($"Either CreateDbContextInstance must be overidden or {typeof(DbContextOptions).Name} needs a constructor with DbContextOptions parameter.");
            var parameter = Expression.Parameter(typeof(DbContextOptions), "options");
            var ctor = Expression.New(constructor, parameter);
            var lambda = Expression.Lambda<Func<DbContextOptions, TDbContext>>(ctor, parameter);
            return lambda.Compile();
        }

        protected virtual TDbContext CreateDbContextInstance(SqliteConnection connection, bool isPrototype)
        {
            var options = new DbContextOptionsBuilder<TDbContext>();
            options.UseSqlite(connection);
            ConfigureDbContextOptionsBuilder(options);

            return _constructor.Value(options.Options);
        }

        protected virtual void ConfigureDbContextOptionsBuilder(DbContextOptionsBuilder<TDbContext> builder)
        {

        }

        protected virtual void PrepareDbContext(TDbContext context)
        {
        }

        #region IDisposable Support

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

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
