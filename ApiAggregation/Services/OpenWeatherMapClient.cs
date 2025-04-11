using ApiAggregation.Services.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ApiAggregation.Services;

public class OpenWeatherMapClient : IExternalApiClient
{
    public string ApiName => "OpenWeatherMap";
    private readonly HttpClient _httpClient;
    private readonly OpenWeatherMapSettings _openWeatherMapSettings;

    public OpenWeatherMapClient(HttpClient httpClient, IOptions<OpenWeatherMapSettings> settings)
    {
        _httpClient = httpClient;
        _openWeatherMapSettings = settings.Value;
    }
    
    public async Task<string> GetDataAsync(IExternalApiFilter filterOptions)
    {
        var response = await _httpClient.GetAsync($"{_openWeatherMapSettings.BaseUrl}weather?q={filterOptions.Keyword}&appid={_openWeatherMapSettings.ApiKey}&units=metric");

        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();
        return content;
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

public class OpenWeatherMapSettings
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
}