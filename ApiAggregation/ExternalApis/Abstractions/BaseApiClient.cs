using System.Text.Json;
using ApiAggregation.ExternalApis.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.Abstractions;

public abstract class BaseApiClient : IExternalApiClient
{
    protected readonly HttpClient HttpClient;
    public abstract string ApiName { get; }
    public ApiSettings Settings { get; set; }

    protected BaseApiClient(IHttpClientFactory httpClientFactory, IOptions<ApiSettings> settings)
    {
        HttpClient = httpClientFactory.CreateClient(ApiName);
        Settings = settings.Value;
    }

    public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AggregatorApi");
        await SetupClient(filterOptions);
        string endpoint = GetEndpoint(filterOptions);

        var response = await HttpClient.GetAsync(endpoint, cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            return new ApiResponse
            {
                ApiName = ApiName,
                IsSuccess = false,
                Content = "Service unavailable",
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
        result.ApiName = ApiName;
        return result;
    }

    protected abstract Task SetupClient(IExternalApiFilter filterOptions);
    protected abstract string GetEndpoint(IExternalApiFilter filterOptions);
}