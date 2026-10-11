namespace NHS111.Application.Commands;

using MediatR;
using NHS111.Application.DTOs.Responses;

public record CreateDispositionCommand(
    string NhsNumber,
    string PatientName,
    DateTime DateOfBirth,
    string DxCode,
    string DosCode,
    string Urgency) : IRequest<DispositionResponse>;
