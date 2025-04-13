using System.Net.Http.Headers;
using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class SpotifySettings : ApiSettings
{
}

public class SpotifyClient(
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifySettings> newsApiSettings,
    ISpotifyTokenService tokenService,
    ILogger<SpotifyClient> logger)
    : BaseApiClient(httpClientFactory, newsApiSettings, logger)
{
    public override string ApiName => "Spotify";

    protected override async Task SetupClient()
    {
        string token = await tokenService.GetAccessTokenAsync();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected override string GetEndpoint(IExternalApiFilter filterOptions)
    {
        string searchUrl = $"search?q={Uri.EscapeDataString(filterOptions.Keyword)}&type=artist";
        return searchUrl;
    }
}