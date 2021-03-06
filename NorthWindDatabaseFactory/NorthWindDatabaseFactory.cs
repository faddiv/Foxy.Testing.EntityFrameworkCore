using Foxy.Testing.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace NorthwindDatabase
{
    public class NorthWindDatabaseFactory : TestDbContextFactory<TestDbContext>
    {
        private readonly DatabaseScaffold _scaffold;
        public static Lazy<string> sqliteLocation = new Lazy<string>(SqliteLocation);

        public event Action Prepared;

        public NorthWindDatabaseFactory(DatabaseScaffold scaffold = null, bool snapshot = false)
            : base(
                  snapshot ? $"Data Source={sqliteLocation.Value};" : "Data Source=:memory:;"
                  )
        {
            _scaffold = scaffold ?? new DatabaseScaffold();
        }

        public DbContextOptionsBuilder<TestDbContext> Builder { get; private set; }

        public TestDbContext MigratedDbContext { get; private set; }

        public bool? LastIsPrototype { get; set; }

        private static string SqliteLocation()
        {
            var directory = Environment.CurrentDirectory;
            var file = GetFile(directory);
            while (!File.Exists(file))
            {
                var parent = new DirectoryInfo(directory).Parent;
                if (parent == null)
                {
                    return GetFile(Environment.CurrentDirectory);
                }
                directory = parent.FullName;
                file = GetFile(directory);
            }
            return file;
        }

        private static string GetFile(string directory)
        {
            return Path.Combine(directory, "prototype.db");
        }

        protected override void PrepareDbContext(TestDbContext context)
        {
            _scaffold.Run(context);
            Prepared?.Invoke();
        }

        protected override bool ShouldRunDatabasePreparation(SqliteConnection prototypeConnection)
        {
            return base.ShouldRunDatabasePreparation(prototypeConnection);
        }

        protected override void ConfigureDbContextOptionsBuilder(DbContextOptionsBuilder<TestDbContext> builder, bool isPrototype)
        {
            Builder = builder;
            LastIsPrototype = isPrototype;
            base.ConfigureDbContextOptionsBuilder(builder, isPrototype);
        }

        protected override void ExecuteMigrate(TestDbContext dbContext)
        {
            MigratedDbContext = dbContext;
            base.ExecuteMigrate(dbContext);
        }
    }
}
