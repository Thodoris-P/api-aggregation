using ApiAggregation.Services.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace ApiAggregation.Infrastructure;

public class CachingExternalApiClientDecorator : IExternalApiClient
{
    private readonly IExternalApiClient _innerClient;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions; 
    
    public CachingExternalApiClientDecorator(
        IExternalApiClient innerClient, 
        HybridCache hybridCache, 
        TimeSpan cacheDuration)
    {
        _innerClient = innerClient;
        _hybridCache = hybridCache;
        _cacheOptions = new HybridCacheEntryOptions { Expiration = cacheDuration, LocalCacheExpiration = cacheDuration };
    }

    public string ApiName { get; }

    public async Task<string> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"{_innerClient.GetType().Name}_{filterOptions}";

        string response =  await _hybridCache.GetOrCreateAsync<string>(
                            cacheKey,
                            async _ => await _innerClient.GetDataAsync(filterOptions, cancellationToken), 
                            _cacheOptions,
                            cancellationToken: cancellationToken);
        
        return response;
    }
}