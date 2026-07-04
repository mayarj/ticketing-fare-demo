using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Api.Tests;

/// <summary>
/// Boots the real API in-memory but re-points its <see cref="TicketingDbContext"/> at a
/// Testcontainers database (swapping the registration directly, which is immune to
/// configuration ordering). Startup still runs the real migrate + seed path.
/// </summary>
public sealed class TicketingApiFactory : WebApplicationFactory<Program>
{
    public required string ConnectionString { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TicketingDbContext>>();
            services.RemoveAll<TicketingDbContext>();
            services.AddDbContext<TicketingDbContext>(options => options.UseSqlServer(ConnectionString));
        });
    }
}
