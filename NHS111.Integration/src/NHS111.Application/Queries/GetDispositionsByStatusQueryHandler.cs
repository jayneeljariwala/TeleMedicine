namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Domain.Interfaces;

public class GetDispositionsByStatusQueryHandler(IDispositionRepository dispositionRepository)
    : IRequestHandler<GetDispositionsByStatusQuery, IEnumerable<DispositionResponse>>
{
    public async Task<IEnumerable<DispositionResponse>> Handle(GetDispositionsByStatusQuery request, CancellationToken cancellationToken)
    {
        var dispositions = await dispositionRepository.GetByStatusAsync(request.Status, cancellationToken);

        return dispositions.Select(d => new DispositionResponse(
            d.Id,
            d.NhsNumber,
            d.PatientName,
            d.DxCode,
            d.DosCode,
            d.Urgency.ToString(),
            d.RoutedTo.ToString(),
            d.Status.ToString(),
            d.CreatedAt,
            d.UpdatedAt));
    }
}
