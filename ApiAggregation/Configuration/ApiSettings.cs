namespace ApiAggregation.Configuration;

public class ApiSettings
{
    public required string ApiKey { get; set; }
    public required string BaseUrl { get; set; }
    public TimeSpan CacheDuration { get; set; }
}