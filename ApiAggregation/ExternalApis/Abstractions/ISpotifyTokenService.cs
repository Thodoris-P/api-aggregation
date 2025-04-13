namespace ApiAggregation.ExternalApis.Abstractions;

public interface ISpotifyTokenService
{
    Task<string> GetAccessTokenAsync();
}