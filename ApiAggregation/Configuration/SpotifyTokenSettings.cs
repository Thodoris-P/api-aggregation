namespace ApiAggregation.Configuration;

public class SpotifyTokenSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public TimeSpan TokenExpiration{ get; init; }
    public required string TokenUrl { get; init; }
    public required string GrantType { get; init; }
}