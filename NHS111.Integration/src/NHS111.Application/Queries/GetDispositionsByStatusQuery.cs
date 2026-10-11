namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Domain.Enums;

public record GetDispositionsByStatusQuery(DispositionStatus Status) : IRequest<IEnumerable<DispositionResponse>>
{
    public GetDispositionsByStatusQuery(string status) : this(Enum.Parse<DispositionStatus>(status, true))
    {
    }
}
