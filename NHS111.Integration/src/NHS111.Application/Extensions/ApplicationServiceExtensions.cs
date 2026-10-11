namespace NHS111.Application.Extensions;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NHS111.Application.Services;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        // 1. Add MediatR scanning NHS111.Application assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // 2. Add FluentValidation scanning NHS111.Application assembly
        services.AddValidatorsFromAssembly(assembly);

        // 3. Register IDispositionRoutingService as scoped
        services.AddScoped<IDispositionRoutingService, DispositionRoutingService>();

        return services;
    }
}
