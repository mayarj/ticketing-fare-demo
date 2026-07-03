using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ticketing.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core command-line tools (e.g. <c>dotnet ef migrations add</c>).
/// Providing this factory means the tools construct the context directly instead of
/// booting the API host — so the startup migrate/seed logic never runs at design time.
/// The connection string is irrelevant for generating migrations from the model.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost,1433;Database=Ticketing;User Id=sa;Password=placeholder;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new TicketingDbContext(options);
    }
}