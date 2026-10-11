namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Exceptions;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;

public class GetDispositionByIdQueryHandler(IDispositionRepository dispositionRepository)
    : IRequestHandler<GetDispositionByIdQuery, DispositionResponse>
{
    public async Task<DispositionResponse> Handle(GetDispositionByIdQuery request, CancellationToken cancellationToken)
    {
        var disposition = await dispositionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (disposition == null)
        {
            throw new NotFoundException(nameof(Disposition), request.Id);
        }

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
