using Microsoft.EntityFrameworkCore;
using Serilog;
using Ummanu.DummyAPI.Data;
using Ummanu.DummyAPI.Data.Repositories;
using Ummanu.DummyAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. UseSerilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. AddDbContext with Npgsql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=ummanu_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<UmmanuDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention();
});

// 3. AddScoped IAppointmentRepository
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();

// 4. AddHostedService<AppointmentSimulationService>
builder.Services.AddHostedService<AppointmentSimulationService>();

// 5. AddControllers
builder.Services.AddControllers();

// 6. AddSwaggerGen
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 7. AddHealthChecks.AddNpgSql
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

// 8. CORS: allow all origins (dummy API)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/healthz");

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<UmmanuDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration on startup failed. Attempting EnsureCreated.");
        try
        {
            var dbContext = services.GetRequiredService<UmmanuDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (Exception fallbackEx)
        {
            logger.LogError(fallbackEx, "EnsureCreated also failed.");
        }
    }
}

app.Run();

public partial class Program { }
