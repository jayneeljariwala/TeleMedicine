namespace NHS111.Application.Services;

using NHS111.Domain.Enums;

public class DispositionRoutingService : IDispositionRoutingService
{
    public Task<RoutingDestination> DetermineRouteAsync(string dxCode, UrgencyLevel urgency)
    {
        if (dxCode.StartsWith("Dx0", StringComparison.OrdinalIgnoreCase) &&
            (urgency == UrgencyLevel.Immediate || urgency == UrgencyLevel.UrgentSameDay))
        {
            return Task.FromResult(RoutingDestination.Ummanu);
        }

        if (dxCode.StartsWith("Dx1", StringComparison.OrdinalIgnoreCase) ||
            dxCode.StartsWith("Dx2", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(RoutingDestination.Adastra);
        }

        return Task.FromResult(RoutingDestination.Both);
    }
}
