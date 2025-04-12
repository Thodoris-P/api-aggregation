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
    Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default);
}

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public string Content { get; set; }
    public bool IsFallback { get; set; }
}