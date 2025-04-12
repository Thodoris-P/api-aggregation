using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis;

public class NewsApiSettings : ApiSettings
{
}

public class NewsClient : IExternalApiClient
{
    public string ApiName => "NewsApi";
    public ApiSettings Settings { get; set; } 
    private readonly HttpClient _httpClient;

    public NewsClient(IHttpClientFactory httpClientFactory, IOptions<NewsApiSettings> newsApiSettings)
    {
        _httpClient = httpClientFactory.CreateClient(ApiName);
        Settings = newsApiSettings.Value;
    }

    public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        string endpoint =
            $"top-headlines?country={filterOptions.Keyword}&apiKey={Settings.ApiKey}";
        
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            return new ApiResponse()
            {
                IsSuccess = true,
                Content = "paparia",
                IsFallback = false
            };
        }
        
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