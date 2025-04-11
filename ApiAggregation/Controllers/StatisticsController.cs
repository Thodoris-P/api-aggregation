using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/statistics")]
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