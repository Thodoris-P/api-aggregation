using ApiAggregation.ExternalApis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Aggregation;

[ApiController]
[Route("api/aggregation")]
[Authorize]
public class AggregationController : ControllerBase
{
    private readonly IAggregatorService _aggregator;
        
    public AggregationController(IAggregatorService aggregatorService)
    {
        _aggregator = aggregatorService;
    }

    [HttpPost]
    public async Task<IActionResult> Get([FromBody]ExternalApiFilter filter)
    {
        var data = await _aggregator.GetAggregatedDataAsync(filter);
        return Ok(data);
    }
}