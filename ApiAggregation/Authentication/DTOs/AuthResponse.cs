namespace ApiAggregation.Authentication.DTOs;

public class AuthResponse
{
    public bool IsSuccessful { get; set; }
    public string Message { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
}