using System.Text.Json;
using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.Abstractions;

public abstract class BaseApiClient : IExternalApiClient
{
    protected readonly HttpClient HttpClient;
    public abstract string ApiName { get; }
    public ApiSettings Settings { get; }
    private readonly ILogger<BaseApiClient> _logger;
    private static string _fallbackMessage = "Service unavailable";
    
    protected BaseApiClient(IHttpClientFactory httpClientFactory, IOptions<ApiSettings> settings, ILogger<BaseApiClient> logger)
    {
        HttpClient = httpClientFactory.CreateClient(ApiName);
        Settings = settings.Value;
        _logger = logger;
    }

    public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AggregatorApi");
        await SetupClient();
        string endpoint = GetEndpoint(filterOptions);

        var response = await HttpClient.GetAsync(endpoint, cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while calling {ApiName} API: {StatusCode}", ApiName, response.StatusCode);
            return new ApiResponse
            {
                ApiName = ApiName,
                IsSuccess = false,
                Content = _fallbackMessage,
                IsFallback = false
            };
        }

        bool isFallback = response.Headers.TryGetValues("X-Fallback-Response", out var values) &&
                          values.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase));

        string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!isFallback)
            return new ApiResponse
            {
                ApiName = ApiName,
                IsSuccess = true,
                Content = jsonContent,
                IsFallback = false
            };
        
        var result = JsonSerializer.Deserialize<ApiResponse>(jsonContent);
        if (result == null)
        {
            _logger.LogWarning("Deserialization returned null for {ApiName}", ApiName);
            return new ApiResponse
            {
                ApiName = ApiName,
                IsSuccess = false,
                Content = _fallbackMessage,
                IsFallback = true
            };
        }
        result.ApiName = ApiName;
        return result;
    }

    protected abstract Task SetupClient();
    protected abstract string GetEndpoint(IExternalApiFilter filterOptions);
}