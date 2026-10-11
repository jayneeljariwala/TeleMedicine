namespace NHS111.Application.Commands;

using MediatR;
using NHS111.Application.DTOs.Responses;

public record UpdateDispositionStatusCommand(
    Guid DispositionId,
    string NewStatus,
    string UpdatedBy,
    string Notes) : IRequest<DispositionResponse>;
