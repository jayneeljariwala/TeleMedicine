namespace NHS111.Application.Commands;

using MassTransit;
using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Services;
using NHS111.Contracts.Events;
using NHS111.Domain.Entities;
using NHS111.Domain.Enums;
using NHS111.Domain.Interfaces;

#pragma warning disable CS9113
public class CreateDispositionCommandHandler(
    IDispositionRepository dispositionRepository,
    IPatientRepository patientRepository,
    IDispositionRoutingService routingService,
    IAuditRepository auditRepository,
    ISendEndpointProvider sendEndpointProvider) : IRequestHandler<CreateDispositionCommand, DispositionResponse>
#pragma warning restore CS9113
{
    public async Task<DispositionResponse> Handle(CreateDispositionCommand request, CancellationToken cancellationToken)
    {
        // 1. Parse UrgencyLevel from request string
        var urgency = Enum.Parse<UrgencyLevel>(request.Urgency, true);

        // 2. Call DetermineRouteAsync → get RoutingDestination
        var routedTo = await routingService.DetermineRouteAsync(request.DxCode, urgency);

        // 3. Create Disposition entity, set RoutedTo and Status=Pending
        var disposition = new Disposition(
            Guid.NewGuid(),
            request.NhsNumber,
            request.PatientName,
            request.DateOfBirth,
            request.DxCode,
            request.DosCode,
            urgency,
            routedTo,
            DispositionStatus.Pending);

        // 4. Call IDispositionRepository.AddAsync
        await dispositionRepository.AddAsync(disposition, cancellationToken);

        // 5. Log to AuditLog: Action="DispositionCreated", PerformedBy="System"
        var auditLog = new AuditLog(
            Guid.NewGuid(),
            disposition.Id,
            "DispositionCreated",
            DateTime.UtcNow,
            $"Disposition created with DxCode {disposition.DxCode} and routed to {routedTo}.",
            "System");
        await auditRepository.LogAsync(auditLog, cancellationToken);

        // 6. Publish DispositionCreatedEvent to correct queue(s)
        var @event = new DispositionCreatedEvent(
            disposition.Id,
            disposition.NhsNumber,
            disposition.PatientName,
            disposition.DateOfBirth,
            disposition.DxCode,
            disposition.DosCode,
            disposition.Urgency.ToString(),
            disposition.RoutedTo.ToString(),
            disposition.CreatedAt);

        if (routedTo == RoutingDestination.Ummanu || routedTo == RoutingDestination.Both)
        {
            var ummanuEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:disposition.ummanu"));
            await ummanuEndpoint.Send(@event, cancellationToken);
        }

        if (routedTo == RoutingDestination.Adastra || routedTo == RoutingDestination.Both)
        {
            var adastraEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:disposition.adastra"));
            await adastraEndpoint.Send(@event, cancellationToken);
        }

        // 7. Update Status=InQueue, call UpdateAsync
        disposition.UpdateStatus(DispositionStatus.InQueue);
        await dispositionRepository.UpdateAsync(disposition, cancellationToken);

        // 8. Map to DispositionResponse and return
        return new DispositionResponse(
            disposition.Id,
            disposition.NhsNumber,
            disposition.PatientName,
            disposition.DxCode,
            disposition.DosCode,
            disposition.Urgency.ToString(),
            disposition.RoutedTo.ToString(),
            disposition.Status.ToString(),
            disposition.CreatedAt,
            disposition.UpdatedAt);
    }
}
