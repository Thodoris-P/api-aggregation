namespace ApiAggregation.ExternalApis.Models;

public class ApiResponse
{
    public string ApiName { get; set; }
    public bool IsSuccess { get; set; }
    public string Content { get; set; }
    public bool IsFallback { get; set; }
}