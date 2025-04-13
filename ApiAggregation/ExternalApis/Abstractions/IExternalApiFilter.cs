namespace ApiAggregation.ExternalApis.Abstractions;

public interface IExternalApiFilter
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}