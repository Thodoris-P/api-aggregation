using ApiAggregation.Authentication.Contracts;

namespace ApiAggregation.Authentication.Abstractions;

public interface IAccountService
{
    AuthResponse Register(AuthRequest request);
    AuthResponse Login(AuthRequest request);
    AuthResponse RefreshToken(string refreshToken);
}