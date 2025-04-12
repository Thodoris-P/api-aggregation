using System.Text.Json;
using ApiAggregation.ExternalApis.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiAggregation.ExternalApis;

public class NewsApiSettings : ApiSettings
{
}

public class NewsClient : BaseApiClient
{
    public NewsClient(IHttpClientFactory httpClientFactory, IOptions<NewsApiSettings> settings) 
        : base(httpClientFactory, settings)
    {
    }

    public override string ApiName => "NewsApi";


    protected override Task SetupClient(IExternalApiFilter filterOptions)
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