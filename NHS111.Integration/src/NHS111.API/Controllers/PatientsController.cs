namespace NHS111.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Queries;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController(
    ISender mediator,
    IPatientRepository patientRepository) : ControllerBase
{
    [HttpGet("{nhsNumber}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNhsNumber([FromRoute] string nhsNumber, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPatientQuery(nhsNumber), ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var patient = new Patient(
            Guid.NewGuid(),
            request.NhsNumber,
            request.Name,
            request.DateOfBirth,
            request.PhoneNumber,
            request.Email);

        await patientRepository.AddAsync(patient, ct);

        var response = new PatientResponse(
            patient.Id,
            patient.NhsNumber,
            patient.Name,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email);

        return CreatedAtAction(nameof(GetByNhsNumber), new { nhsNumber = patient.NhsNumber }, response);
    }
}
