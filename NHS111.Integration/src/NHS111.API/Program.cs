using System.Text;
using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NHS111.API.Middleware;
using NHS111.Application.Extensions;
using NHS111.Infrastructure.Extensions;
using NHS111.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. UseSerilog() from configuration
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// 2. AddApplication() from NHS111.Application
builder.Services.AddApplication();

// 3. AddInfrastructure() from NHS111.Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// 4. AddControllers with FluentValidation
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

// 5. AddAuthentication with JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? "NHS111-Super-Secret-Key-2024-ChangeInProd";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "NHS111.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "NHS111.Clients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();

// 6. AddRateLimiter (fixed window: 100 req/min per IP)
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// 7. AddHealthChecks (.AddNpgSql, .AddRedis, .AddRabbitMQ)
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=postgres;Port=5432;Database=nhs111;Username=postgres;Password=postgres";
var redisConn = builder.Configuration.GetConnectionString("Redis")
    ?? "redis:6379";
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq";
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";
var rabbitMqUri = new Uri($"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:5672/");

builder.Services.AddHealthChecks()
    .AddNpgSql(defaultConn)
    .AddRedis(redisConn)
    .AddRabbitMQ(rabbitMqUri);

// 8. AddSwaggerGen with JWT security definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NHS111.API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 9. Use RequestLoggingMiddleware
app.UseMiddleware<RequestLoggingMiddleware>();

// 10. Use GlobalExceptionMiddleware
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 11. Use Authentication
app.UseAuthentication();

// 12. Use Authorization
app.UseAuthorization();

// 13. Use RateLimiter
app.UseRateLimiter();

// 14. Map Controllers
app.MapControllers();

// 15. Map HealthChecks
app.MapHealthChecks("/health");

// 16. Apply EF Core migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NHS111DbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while applying EF Core migrations on startup.");
}

app.Run();

public partial class Program { }
