using CustomCharInfo.server.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustomCharInfo.server.Tests.TestHelpers
{
    /// <summary>
    /// Builds an isolated AppDbContext backed by a fresh SQLite in-memory database per test.
    /// The connection must stay open for the lifetime of the context, and the caller owns
    /// disposal of both (this class implements IDisposable to make that easy).
    /// </summary>
    public sealed class TestDbContextFactory : IDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }

        public TestDbContextFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
