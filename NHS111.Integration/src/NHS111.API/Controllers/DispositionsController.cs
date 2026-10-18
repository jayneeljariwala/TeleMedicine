namespace NHS111.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHS111.Application.Commands;
using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Queries;

[ApiController]
[Route("api/dispositions")]
[Authorize]
public class DispositionsController(ISender mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DispositionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDispositionRequest request, CancellationToken ct)
    {
        var command = new CreateDispositionCommand(
            request.NhsNumber,
            request.PatientName,
            request.DateOfBirth,
            request.DxCode,
            request.DosCode,
            request.Urgency);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DispositionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDispositionByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<DispositionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPendingDispositionsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<DispositionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus([FromRoute] string status, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDispositionsByStatusQuery(status), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(DispositionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateDispositionStatusRequest request, CancellationToken ct)
    {
        var command = new UpdateDispositionStatusCommand(id, request.NewStatus, request.UpdatedBy, request.Notes);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }
}
