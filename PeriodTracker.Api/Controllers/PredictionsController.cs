using Microsoft.AspNetCore.Mvc;
using PeriodTracker.Api.Models;
using PeriodTracker.Api.Services;

namespace PeriodTracker.Api.Controllers;

[ApiController]
[Route("api/predictions")]
public sealed class PredictionsController(PredictionService predictionService) : ControllerBase
{
    [HttpGet("get-prediction/{userId:guid}")]
    public async Task<ActionResult<PredictionResponse>> GetPrediction(Guid userId)
    {
        var prediction = await predictionService.PredictAsync(userId);
        return Ok(prediction);
    }
}
