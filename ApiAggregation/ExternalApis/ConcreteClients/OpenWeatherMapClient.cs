using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class OpenWeatherMapSettings : ApiSettings
{
}

public class OpenWeatherMapClient(IHttpClientFactory httpClientFactory, IOptions<OpenWeatherMapSettings> settings)
    : BaseApiClient(httpClientFactory, settings)
{
    public override string ApiName => "OpenWeatherMap";

    protected override Task SetupClient(IExternalApiFilter filterOptions)
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

