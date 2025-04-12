namespace ApiAggregation.ExternalApis;

public interface IExternalApiFilter
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class ExternalApiFilter : IExternalApiFilter
{
    public string? Keyword { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

public interface IExternalApiClient
{
    string ApiName { get; }
    public ApiSettings Settings { get; set; }
    Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default);
}

public class ApiResponse
{
    public string ApiName { get; set; }
    public bool IsSuccess { get; set; }
    public string Content { get; set; }
    public bool IsFallback { get; set; }
}