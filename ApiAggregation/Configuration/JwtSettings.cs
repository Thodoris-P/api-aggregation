namespace ApiAggregation.Configuration;

public class JwtSettings
{
    public required string Key { get; init; }
    public int ExpiryInMinutes { get; init; }
    public int RefreshTokenExpiryInDays { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
}