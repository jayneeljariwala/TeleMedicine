namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;

public record GetDispositionByIdQuery(Guid Id) : IRequest<DispositionResponse>;
