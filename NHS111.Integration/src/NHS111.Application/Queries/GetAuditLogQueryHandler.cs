namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Domain.Interfaces;

public class GetAuditLogQueryHandler(IAuditRepository auditRepository)
    : IRequestHandler<GetAuditLogQuery, IEnumerable<AuditLogResponse>>
{
    public async Task<IEnumerable<AuditLogResponse>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var logs = await auditRepository.GetByDispositionIdAsync(request.DispositionId, cancellationToken);

        return logs.Select(a => new AuditLogResponse(
            a.Id,
            a.DispositionId,
            a.Action,
            a.PerformedAt,
            a.Details,
            a.PerformedBy));
    }
}
