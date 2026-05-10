using Microsoft.AspNetCore.Mvc;
using PeriodTracker.Api.Models;
using PeriodTracker.Api.Services;

namespace PeriodTracker.Api.Controllers;

[ApiController]
[Route("api/periods")]
public sealed class PeriodsController(PeriodService periodService) : ControllerBase
{
    [HttpGet("get-period-history/{userId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PeriodResponse>>> ListPeriods(Guid userId)
    {
        var periods = await periodService.ListPeriodsAsync(userId);
        return Ok(periods);
    }

    [HttpPost("add-period-entry/{userId:guid}")]
    public async Task<ActionResult<PeriodResponse>> CreatePeriod(Guid userId, CreatePeriodRequest request)
    {
        try
        {
            var created = await periodService.CreatePeriodAsync(userId, request);
            return Created($"/api/periods/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiError(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiError(ex.Message));
        }
    }
}
