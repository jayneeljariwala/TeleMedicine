namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;

public record GetPatientQuery(string NhsNumber) : IRequest<PatientResponse>;
