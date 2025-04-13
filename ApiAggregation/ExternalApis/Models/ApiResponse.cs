namespace ApiAggregation.ExternalApis.Models;

public class ApiResponse
{
    public string ApiName { get; set; }
    public bool IsSuccess { get; init; }
    public string Content { get; init; }
    public bool IsFallback { get; init; }
}