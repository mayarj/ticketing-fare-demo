using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Ticketing.Infrastructure.Persistence;
using Xunit;

namespace Ticketing.TestSupport;

/// <summary>
/// Spins up a single SQL Server 2019 container for the test assembly (shared via an
/// xUnit collection). Each test asks for its own uniquely-named, freshly-migrated
/// database so tests never contaminate one another despite the unique-code constraints.
/// The 2019 image is pinned to match the brief and because it is already cached locally.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates a new database, applies the EF migration, and returns a handle to it.</summary>
    public async Task<TestDatabase> CreateDatabaseAsync()
    {
        var databaseName = "ticketing_test_" + Guid.NewGuid().ToString("N");
        var connectionString = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = databaseName
        }.ConnectionString;

        var database = new TestDatabase(connectionString);
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();
        return database;
    }
}

/// <summary>A single isolated test database. Hand out fresh contexts bound to it, which
/// is how the point-in-time tests read a ticket back through a second context.</summary>
public sealed class TestDatabase
{
    private readonly string _connectionString;

    public TestDatabase(string connectionString) => _connectionString = connectionString;

    public string ConnectionString => _connectionString;

    public TicketingDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new TicketingDbContext(options);
    }
}
