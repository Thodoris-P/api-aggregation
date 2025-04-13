using ApiAggregation.Statistics.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Statistics.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await statisticsService.GetApiStatistics());
    }
}