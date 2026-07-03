using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Services;
using Ticketing.Domain.Modifications;
using Ticketing.Domain.Policies;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Repositories;
using Ticketing.Infrastructure.Seeding;
using Ticketing.Infrastructure.Services;

namespace Ticketing.Api.Extensions;

/// <summary>
/// Composition root helpers: one method registers the Application (pure calculation)
/// services, the other registers Infrastructure (EF Core, repositories, adapters).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Base-fare strategies are stateless — register each and resolve by product type.
        services.AddSingleton<IFarePolicy, PointToPointFarePolicy>();
        services.AddSingleton<IFarePolicy, DailyPassFarePolicy>();
        services.AddSingleton<FarePolicyResolver>();
        services.AddSingleton<ModificationApplier>();

        services.AddScoped<FareCalculatorFactory>();
        services.AddScoped<ITicketIssuer, TicketIssuer>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string 'Default'.");

        services.AddDbContext<TicketingDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<ICurrentFareRateRepository, CurrentFareRateRepository>();
        services.AddScoped<IModificationRuleRepository, ModificationRuleRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDistanceProvider, InMemoryDistanceProvider>();
        services.AddSingleton<ITicketNumberGenerator, TicketNumberGenerator>();

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}