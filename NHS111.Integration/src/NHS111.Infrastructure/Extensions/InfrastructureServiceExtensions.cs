namespace NHS111.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NHS111.Domain.Interfaces;
using NHS111.Infrastructure.Http;
using NHS111.Infrastructure.Messaging;
using NHS111.Infrastructure.Persistence;
using NHS111.Infrastructure.Persistence.Repositories;
using NHS111.Infrastructure.Security;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Register DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? "Host=localhost;Port=5432;Database=nhs111;Username=postgres;Password=1234;";

        services.AddDbContext<NHS111DbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention();
        });

        // 2. Register Redis ConnectionMultiplexer as Singleton
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["ConnectionStrings:Redis"]
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configOptions = ConfigurationOptions.Parse(redisConnectionString);
            configOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // 3. Register Repositories as Scoped
        services.AddScoped<IDispositionRepository, DispositionRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Security Services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();

        // 4. Register Typed HTTP Clients with Polly Retry Policy (3 retries: 2s, 4s, 8s)
        services.AddHttpClient<UmmanuHttpClient>(client =>
        {
            var baseUrl = configuration["ExternalApis:UmmanuBaseUrl"] ?? "http://localhost:7001/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
        }).AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<AdastraHttpClient>(client =>
        {
            var baseUrl = configuration["ExternalApis:AdastraBaseUrl"] ?? "http://localhost:7002/";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
        }).AddPolicyHandler(GetRetryPolicy());

        // 5. Register MassTransit
        services.AddNhs111MassTransit(configuration);

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
