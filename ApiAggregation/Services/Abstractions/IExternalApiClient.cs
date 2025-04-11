namespace ApiAggregation.Services.Abstractions;

public interface IExternalApiFilter
{
    public string? Keyword { get; set; }
}

public class ExternalApiFilter : IExternalApiFilter
{
    public string? Keyword { get; set; }
}

public interface IExternalApiClient
{
    string ApiName { get; }
    Task<string> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default);
}