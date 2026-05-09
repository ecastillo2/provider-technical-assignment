using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProviderAssignmentStarter.Infrastructure.Data;
using ProviderAssignmentStarter.Infrastructure.Interceptors;

namespace ProviderAssignmentStarter.Tests.TestHelpers;

/// <summary>
/// Builds an isolated <see cref="AppDbContext"/> backed by SQLite
/// in-memory ("DataSource=:memory:"), with the same interceptors that
/// production uses. Tests get a clean schema per call and share no state.
///
/// Why a real SQLite in-memory and not the EF InMemory provider?
///   - InMemory ignores relational features like CHECK constraints,
///     unique indexes, and the global query filter operates differently.
///   - SQLite in-memory exercises real SQL and the real save pipeline,
///     so the soft-delete and cascade behaviour we are testing is the
///     same code path that runs in production.
/// </summary>
internal sealed class TestDb : IDisposable
{
    public AppDbContext Context { get; }
    private readonly SqliteConnection _connection;

    public TestDb()
    {
        // The connection must be kept open for the lifetime of the test;
        // SQLite drops the in-memory database the moment the last
        // connection closes.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new SoftDeleteInterceptor(), new AuditableInterceptor())
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Adds <paramref name="entity"/>, persists with SaveChanges so the
    /// generated key is populated, and returns the materialised entity.
    /// </summary>
    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        Context.Add(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
