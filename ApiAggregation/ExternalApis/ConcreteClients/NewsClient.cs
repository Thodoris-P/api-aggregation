using ApiAggregation.Configuration;
using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis.ConcreteClients;

public class NewsApiSettings : ApiSettings
{
}

public class NewsClient(IHttpClientFactory httpClientFactory, IOptions<NewsApiSettings> settings, ILogger<NewsClient> logger)
    : BaseApiClient(httpClientFactory, settings, logger)
{
    public override string ApiName => "NewsApi";


    protected override Task SetupClient()
    {
        return Task.FromResult(0);
    }

    protected override string GetEndpoint(IExternalApiFilter filterOptions)
    {
        string endpoint =
            $"top-headlines?country={filterOptions.Keyword}&apiKey={Settings.ApiKey}";
        return endpoint;
    }
}