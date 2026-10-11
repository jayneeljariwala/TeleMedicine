namespace NHS111.Application.Queries;

using MediatR;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Exceptions;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;

public class GetPatientQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientQuery, PatientResponse>
{
    public async Task<PatientResponse> Handle(GetPatientQuery request, CancellationToken cancellationToken)
    {
        var patient = await patientRepository.GetByNhsNumberAsync(request.NhsNumber, cancellationToken);
        if (patient == null)
        {
            throw new NotFoundException(nameof(Patient), request.NhsNumber);
        }

        return new PatientResponse(
            patient.Id,
            patient.NhsNumber,
            patient.Name,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email);
    }
}
