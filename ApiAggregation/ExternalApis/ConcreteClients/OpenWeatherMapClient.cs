using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class OpenWeatherMapSettings : ApiSettings
{
}

public class OpenWeatherMapClient(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenWeatherMapSettings> settings,
    ILogger<OpenWeatherMapClient> logger)
    : BaseApiClient(httpClientFactory, settings, logger)
{
    public override string ApiName => "OpenWeatherMap";

    protected override Task SetupClient()
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

