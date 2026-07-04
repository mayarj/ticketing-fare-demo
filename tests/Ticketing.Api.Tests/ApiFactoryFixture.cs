using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Api.Tests;

/// <summary>
/// One SQL Server 2019 container for the whole API test assembly. Provides a shared,
/// already-migrated-and-seeded API + client for the read/validation tests, and can mint
/// fully isolated API instances (own database) for tests that mutate current data.
/// </summary>
public sealed class ApiFactoryFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
        .Build();

    private TicketingApiFactory? _shared;

    public HttpClient Client { get; private set; } = default!;

    public IServiceProvider Services => _shared!.Services;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _shared = new TicketingApiFactory { ConnectionString = NewConnectionString() };
        Client = _shared.CreateClient(); // triggers migrate + seed on the shared database
    }

    public async Task DisposeAsync()
    {
        _shared?.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>A fresh API on its own migrated + seeded database, isolated from all others.</summary>
    public IsolatedApi CreateIsolatedApi()
    {
        var connectionString = NewConnectionString();
        return new IsolatedApi(new TicketingApiFactory { ConnectionString = connectionString }, connectionString);
    }

    private string NewConnectionString() =>
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "api_test_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
}

/// <summary>An isolated API instance plus direct DB access to its own database.</summary>
public sealed class IsolatedApi : IDisposable
{
    private readonly TicketingApiFactory _factory;

    public IsolatedApi(TicketingApiFactory factory, string connectionString)
    {
        _factory = factory;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public HttpClient CreateClient() => _factory.CreateClient();

    public TicketingDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TicketingDbContext>().UseSqlServer(ConnectionString).Options);

    public void Dispose() => _factory.Dispose();
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactoryFixture>;
