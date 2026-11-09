namespace Ummanu.DummyAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Ummanu.DummyAPI.Data;

[ApiController]
[Route("health")]
public class HealthController(UmmanuDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (canConnect)
            {
                return Ok(new { status = "Healthy", database = "Connected" });
            }

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy", database = "Disconnected" });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy", error = ex.Message });
        }
    }
}

