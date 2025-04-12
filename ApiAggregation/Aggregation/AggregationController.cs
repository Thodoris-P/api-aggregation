using ApiAggregation.ExternalApis;
using ApiAggregation.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Aggregation;

[ApiController]
[Route("api/aggregation")]
[Authorize]
[TrackPerformance]
public class AggregationController(IAggregatorService aggregatorService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Get([FromBody]ExternalApiFilter filter)
    {
        var data = await aggregatorService.GetAggregatedDataAsync(filter);
        return Ok(data);
    }
}