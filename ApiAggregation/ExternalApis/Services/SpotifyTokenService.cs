using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.Services;

public class SpotifyTokenService(
    HttpClient httpClient,
    HybridCache hybridCache,
    IOptions<SpotifyTokenSettings> settings)
    : ISpotifyTokenService
{
    private readonly SpotifyTokenSettings _settings = settings.Value;


    public async Task<string> GetAccessTokenAsync()
    {
        string token = await hybridCache.GetOrCreateAsync<string>(
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
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var requestData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", _settings.GrantType)
        ]);

        var response = await httpClient.PostAsync(_settings.TokenUrl, requestData);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        string? accessToken = root.GetProperty("access_token").GetString();

        return accessToken;
    }
}