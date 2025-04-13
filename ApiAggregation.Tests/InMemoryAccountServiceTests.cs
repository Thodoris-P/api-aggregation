using ApiAggregation.Authentication;
using ApiAggregation.Authentication.Contracts;
using ApiAggregation.Authentication.Models;
using ApiAggregation.Authentication.Services;
using ApiAggregation.Configuration;
using ApiAggregation.UnitTests.Fakes;
using Bogus;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ApiAggregation.UnitTests;

public class InMemoryAccountServiceTests : IDisposable
{
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly JwtSettings _jwtSettings;
    private readonly Faker _faker;
    private const string InvalidRefreshToken = "InvalidToken";
    private readonly string _username;
    private readonly string _password;
    private readonly InMemoryAccountService _accountService;
    private readonly AuthRequest _defaultUser;


    public InMemoryAccountServiceTests()
    {
        var fixedTime = DateTime.UtcNow;
        _dateTimeProvider = new FakeDateTimeProvider(fixedTime);

        // Setup a dummy JWT settings object.
        _jwtSettings = new JwtSettings
        {
            ExpiryInMinutes = 60,
            RefreshTokenExpiryInDays = 7,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "0123456789abcdef0123456789abcdef" 
        };
        _accountService = new InMemoryAccountService(_dateTimeProvider, Options.Create(_jwtSettings));
        _faker = new Faker();
        _username = _faker.Internet.UserName();
        _password = _faker.Internet.Password();
        _defaultUser = new AuthRequest(_username, _password);
    }
    
    [Fact]
    public void Register_NewUser_ShouldSucceed()
    {
        // Arrange

        // Act
        var response = _accountService.Register(_defaultUser);

        // Assert
        response.IsSuccessful.ShouldBeTrue();
        response.Message.ShouldBe("User registered successfully");
    }
    
    [Fact]
    public void Register_SameUser_ShouldFail()
    {
        // Arrange
        _ = _accountService.Register(_defaultUser);

        // Act
        var response = _accountService.Register(_defaultUser);

        // Assert
        response.IsSuccessful.ShouldBeFalse();
        response.Message.ShouldBe("User already exists");
    }

    [Fact]
    public void Login_NonExistentUser_ShouldFail()
    {
        // Arrange
        
        // Act
        var response = _accountService.Login(_defaultUser);

        // Assert
        response.IsSuccessful.ShouldBeFalse();
        response.Message.ShouldBe("Invalid credentials");
    }

    [Fact]
    public void Login_InvalidPassword_ShouldFail()
    {
        // Arrange
        string wrongPassword = _faker.Internet.Password();
        _accountService.Register(_defaultUser);
        var invalidUser = new AuthRequest(_username, wrongPassword);

        // Act
        var response = _accountService.Login(invalidUser);

        // Assert
        response.IsSuccessful.ShouldBeFalse();
        response.Message.ShouldBe("Invalid credentials");
    }

    [Fact]
    public void Login_ValidCredentials_ShouldSucceed()
    {
        // Arrange
        _ = _accountService.Register(_defaultUser);

        // Act
        var loginResponse = _accountService.Login(_defaultUser);

        // Assert
        loginResponse.IsSuccessful.ShouldBeTrue();
        loginResponse.Message.ShouldBe("Login successful");
        loginResponse.Token.ShouldNotBeNullOrWhiteSpace();
        loginResponse.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }
    
    [Fact]
    public void RefreshToken_ValidToken_ShouldSucceed()
    {
        // Arrange
        // Register and login to generate tokens
        _accountService.Register(_defaultUser);
        var loginResponse = _accountService.Login(_defaultUser);
        string refreshToken = loginResponse.RefreshToken;

        // Act
        var refreshResponse = _accountService.RefreshToken(refreshToken);

        // Assert
        refreshResponse.IsSuccessful.ShouldBeTrue();
        refreshResponse.Message.ShouldBe("Token refreshed successfully");
        refreshResponse.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        refreshResponse.Token.ShouldNotBeNullOrWhiteSpace();
        refreshResponse.RefreshToken.ShouldNotBe(refreshToken);
    }

    [Fact]
    public void RefreshToken_InvalidToken_ShouldFail()
    {
        // Act
        var response = _accountService.RefreshToken(InvalidRefreshToken);

        // Assert
        response.IsSuccessful.ShouldBeFalse();
        response.Message.ShouldBe("Invalid or expired refresh token");
    }

    [Fact]
    public void RefreshToken_ExpiredToken_ShouldFail()
    {
        // Arrange
        _accountService.Register(_defaultUser);
        var loginResponse = _accountService.Login(_defaultUser);
        // Simulate token expiry by advancing the UtcNow beyond the refresh token expiry
        _dateTimeProvider.Advance(TimeSpan.FromDays(_jwtSettings.RefreshTokenExpiryInDays + 1));

        // Act
        var response = _accountService.RefreshToken(loginResponse.RefreshToken);

        // Assert
        response.IsSuccessful.ShouldBeFalse();
        response.Message.ShouldBe("Invalid or expired refresh token");
    }

    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _accountService.Reset();
    }
}
