using System.Net;
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
    
    public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        string endpoint =
            $"weather?q={filterOptions.Keyword}&appid={_openWeatherMapSettings.ApiKey}&units=metric";
        
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        // Determine if this is a fallback response by checking the custom header
        bool isFallback = response.Headers.TryGetValues("X-Fallback-Response", out var values) &&
                          values.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase));

        string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!isFallback)
            return new ApiResponse()
            {
                IsSuccess = true,
                Content = jsonContent,
                IsFallback = false
            };
        
        var fallback = JsonSerializer.Deserialize<ApiResponse>(jsonContent);
        return fallback;
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
    public TimeSpan CacheDuration { get; set; }
}