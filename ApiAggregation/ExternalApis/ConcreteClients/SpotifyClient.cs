using System.Net.Http.Headers;
using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.Models;
using ApiAggregation.ExternalApis.Services;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class SpotifySettings : ApiSettings
{
}

public class SpotifyClient(
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifySettings> newsApiSettings,
    ISpotifyTokenService tokenService)
    : BaseApiClient(httpClientFactory, newsApiSettings)
{
    public override string ApiName => "Spotify";

    protected override async Task SetupClient(IExternalApiFilter filterOptions)
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