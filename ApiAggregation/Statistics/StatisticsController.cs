using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Statistics;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(statisticsService.GetApiStatistics());
    }
}