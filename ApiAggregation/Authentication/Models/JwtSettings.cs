namespace ApiAggregation.Authentication.Models;

public class JwtSettings
{
    public string Key { get; set; } = null!;
    public int ExpiryInMinutes { get; set; }
    public int RefreshTokenExpiryInDays { get; set; }
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
}