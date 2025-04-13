namespace ApiAggregation.Configuration;

public class SpotifyTokenSettings
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public TimeSpan TokenExpiration{ get; set; }
    public string TokenUrl { get; set; }
    public string GrantType { get; set; }
}