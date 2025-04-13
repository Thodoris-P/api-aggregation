using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ApiAggregation.Authentication.Abstractions;
using ApiAggregation.Authentication.DTOs;
using ApiAggregation.Authentication.Models;
using ApiAggregation.Infrastructure.Abstractions;
using ApiAggregation.Infrastructure.Providers;
using ApiAggregation.Statistics;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiAggregation.Authentication.Services;

public class InMemoryAccountService(IDateTimeProvider dateTimeProvider, IOptions<JwtSettings> jwtSettings)
    : IAccountService
{
    private readonly List<User> _users = [];
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public AuthResponse Register(string username, string password)
    {
        if (_users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            return new AuthResponse
            {
                IsSuccessful = false,
                Message = "User already exists",
            };
        }

        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };

        _users.Add(user);

        return new AuthResponse {
            IsSuccessful = true,
            Message = "User registered successfully"
        };
    }
    
    public AuthResponse Login(string username, string password)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null || !VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            return new AuthResponse{
                IsSuccessful = false,
                Message = "Invalid credentials"
                
            };
        }
        
        string? jwtToken = GenerateJwtToken(user);
        string refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        int refreshExpiryInDays = _jwtSettings.RefreshTokenExpiryInDays;
        user.RefreshTokenExpiryTime = dateTimeProvider.UtcNow.AddDays(refreshExpiryInDays);
        return new AuthResponse
        {
            IsSuccessful = true,
            Message = "Login successful",
            Token = jwtToken,
            RefreshToken = refreshToken,
        };
    }

    public AuthResponse RefreshToken(string refreshToken)
    {
        var user = _users.FirstOrDefault(u => u.RefreshToken == refreshToken);
        if (user == null || user.RefreshTokenExpiryTime <= dateTimeProvider.UtcNow)
        {
            return new AuthResponse
            {
                IsSuccessful = false,
                Message = "Invalid or expired refresh token",
            };
        }

        string token = GenerateJwtToken(user);
        string newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        int refreshExpiryInDays = _jwtSettings.RefreshTokenExpiryInDays;
        user.RefreshTokenExpiryTime = dateTimeProvider.UtcNow.AddDays(refreshExpiryInDays);

        return new AuthResponse {
            IsSuccessful = true,
            Message = "Token refreshed successfully",
            Token = token,
            RefreshToken = newRefreshToken
        };
    }
    
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        int expiryInMinutes = _jwtSettings.ExpiryInMinutes;
        byte[] key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            ]),
            Expires = dateTimeProvider.UtcNow.AddMinutes(expiryInMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(storedHash);
    }
    
    private static string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    public void Reset()
    {
        _users.Clear();
    }
}