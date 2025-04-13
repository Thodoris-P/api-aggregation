namespace ApiAggregation.Configuration;

public class ApiSettings
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public TimeSpan CacheDuration { get; set; }
}