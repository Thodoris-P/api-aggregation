using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class SpotifyClient : BaseApiClient
{
    public override string ApiName => "Spotify";
    private readonly ISpotifyTokenService _tokenService;

    public SpotifyClient(IHttpClientFactory httpClientFactory, IOptions<SpotifySettings> newsApiSettings,
        ISpotifyTokenService tokenService) : base(httpClientFactory, newsApiSettings)
    {
        _tokenService = tokenService;
    }
    
    protected override async Task SetupClient(IExternalApiFilter filterOptions)
    {
        string token = await _tokenService.GetAccessTokenAsync();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected override string GetEndpoint(IExternalApiFilter filterOptions)
    {
        string searchUrl = $"search?q={Uri.EscapeDataString(filterOptions.Keyword)}&type=artist";
        return searchUrl;
    }
}

public interface ISpotifyTokenService
{
    Task<string> GetAccessTokenAsync();
}

public class SpotifyTokenSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public TimeSpan TokenExpiration{ get; set; }
    public string TokenUrl { get; set; }
    public string GrantType { get; set; }
}

public class SpotifyTokenService : ISpotifyTokenService
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyTokenSettings _settings;
    private readonly HybridCache _hybridCache;
    
    public SpotifyTokenService(HttpClient httpClient, HybridCache hybridCache, IOptions<SpotifyTokenSettings> settings)
    {
        _httpClient = httpClient;
        _hybridCache = hybridCache;
        _settings = settings.Value;
    }


    public async Task<string> GetAccessTokenAsync()
    {
        string token = await _hybridCache.GetOrCreateAsync<string>(
            "SpotifyAccessToken",
            async _ => await FetchAccessTokenAsync() ?? string.Empty,
            new HybridCacheEntryOptions{Expiration = _settings.TokenExpiration}
            );
        return token;
    }

    private async Task<string?> FetchAccessTokenAsync()
    {
        byte[] headerValue = Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}");
        string authHeader = Convert.ToBase64String(headerValue);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var requestData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", _settings.GrantType)
        ]);

        var response = await _httpClient.PostAsync(_settings.TokenUrl, requestData);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        string? accessToken = root.GetProperty("access_token").GetString();

        return accessToken;
    }
}

public class SpotifySettings : ApiSettings
{
}