using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Statistics;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsSevice _statisticsService;

    public StatisticsController(IStatisticsSevice statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_statisticsService.GetApiStatistics());
    }
}