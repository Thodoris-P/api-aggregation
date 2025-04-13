using ApiAggregation.ExternalApis.Abstractions;

namespace ApiAggregation.ExternalApis.Models;

public class ExternalApiFilter : IExternalApiFilter
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}