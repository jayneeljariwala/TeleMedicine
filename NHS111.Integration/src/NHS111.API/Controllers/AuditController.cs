namespace NHS111.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Queries;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController(ISender mediator) : ControllerBase
{
    [HttpGet("{dispositionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDispositionId([FromRoute] Guid dispositionId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAuditLogQuery(dispositionId), ct);
        return Ok(result);
    }
}
