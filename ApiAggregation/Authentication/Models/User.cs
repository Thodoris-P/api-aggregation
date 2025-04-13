namespace ApiAggregation.Authentication.Models;

public class User
{
    public Guid Id { get; init; }
    public required string Username { get; init; } 
    public byte[] PasswordHash { get; init; } = [];
    public byte[] PasswordSalt { get; init; } = [];
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}