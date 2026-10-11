namespace NHS111.Application.Commands;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Exceptions;
using NHS111.Domain.Entities;
using NHS111.Domain.Enums;
using NHS111.Domain.Interfaces;

public class UpdateDispositionStatusCommandHandler(
    IDispositionRepository dispositionRepository,
    IAuditRepository auditRepository) : IRequestHandler<UpdateDispositionStatusCommand, DispositionResponse>
{
    public async Task<DispositionResponse> Handle(UpdateDispositionStatusCommand request, CancellationToken cancellationToken)
    {
        // 1. Get disposition by Id (throw NotFoundException if not found)
        var disposition = await dispositionRepository.GetByIdAsync(request.DispositionId, cancellationToken);
        if (disposition == null)
        {
            throw new NotFoundException(nameof(Disposition), request.DispositionId);
        }

        // 2. Parse and update Status
        var newStatus = Enum.Parse<DispositionStatus>(request.NewStatus, true);
        disposition.UpdateStatus(newStatus);

        // 3. Call UpdateAsync
        await dispositionRepository.UpdateAsync(disposition, cancellationToken);

        // 4. Log to AuditLog: Action="StatusUpdated", Details=Notes, PerformedBy=UpdatedBy
        var auditLog = new AuditLog(
            Guid.NewGuid(),
            disposition.Id,
            "StatusUpdated",
            DateTime.UtcNow,
            request.Notes,
            request.UpdatedBy);
        await auditRepository.LogAsync(auditLog, cancellationToken);

        // 5. Return updated DispositionResponse
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
