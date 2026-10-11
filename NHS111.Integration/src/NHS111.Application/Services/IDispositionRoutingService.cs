namespace NHS111.Application.Services;

using NHS111.Domain.Enums;

public interface IDispositionRoutingService
{
    Task<RoutingDestination> DetermineRouteAsync(string dxCode, UrgencyLevel urgency);
}
