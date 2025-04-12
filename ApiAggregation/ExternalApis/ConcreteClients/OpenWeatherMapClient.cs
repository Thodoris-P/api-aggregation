using System.Text.Json;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Options;


namespace ApiAggregation.ExternalApis;

public class OpenWeatherMapClient : BaseApiClient
{
    public override string ApiName => "OpenWeatherMap";

    public OpenWeatherMapClient(IHttpClientFactory httpClientFactory, IOptions<OpenWeatherMapSettings> settings):
        base(httpClientFactory, settings)
    {
    }

    protected override Task SetupClient(IExternalApiFilter filterOptions)
    {
        return Task.FromResult(0);
    }

    protected override string GetEndpoint(IExternalApiFilter filterOptions)
    {
        string endpoint =
            $"weather?q={filterOptions.City}&appid={Settings.ApiKey}&units=metric";
        return endpoint;
    }
}

public abstract class ExternalApiClientException(string message) : Exception(message) { }

public abstract class WeatherServiceException(string message) : ExternalApiClientException(message);

public class WeatherServiceNullResponseException : WeatherServiceException
{
    public IExternalApiFilter FilterOptions { get; set; }
    public WeatherServiceNullResponseException(IExternalApiFilter filterOptions) : base($"The response from the Weather Service was null. Provided FilterOptions: {filterOptions:C}")
    {
        FilterOptions = filterOptions;
    }
}

public class ApiSettings
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public TimeSpan CacheDuration { get; set; }
}

public class OpenWeatherMapSettings : ApiSettings
{
}