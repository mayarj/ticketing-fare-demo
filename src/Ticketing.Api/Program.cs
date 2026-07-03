using Microsoft.EntityFrameworkCore;
using Ticketing.Api.Extensions;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "Ticketing & Fare Calculation API (Task)", Version = "v1" }));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Apply migrations and seed on startup (retrying while SQL Server finishes booting).
await InitialiseDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

static async Task InitialiseDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<TicketingDbContext>();

    const int maxAttempts = 12;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex,
                "Database not ready (attempt {Attempt}/{Max}); retrying in 3s...", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    await services.GetRequiredService<DatabaseSeeder>().SeedAsync();
    logger.LogInformation("Database migrated and seeded.");
}

// Exposed so the API can be referenced from integration tests (WebApplicationFactory).
public partial class Program;