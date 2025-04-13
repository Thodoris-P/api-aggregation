using ApiAggregation.Authentication.DTOs;

namespace ApiAggregation.Authentication.Abstractions;

public interface IAccountService
{
    AuthResponse Register(string username, string password);
    AuthResponse Login(string username, string password);
    AuthResponse RefreshToken(string refreshToken);
}