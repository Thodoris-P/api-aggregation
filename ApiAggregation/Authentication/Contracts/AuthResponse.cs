namespace ApiAggregation.Authentication.Contracts;

public record AuthResponse(bool IsSuccessful, string Message, string? Token, string? RefreshToken);