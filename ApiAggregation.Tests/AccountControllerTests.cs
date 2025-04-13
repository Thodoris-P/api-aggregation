using ApiAggregation.Authentication;
using ApiAggregation.Authentication.Abstractions;
using ApiAggregation.Authentication.Contracts;
using ApiAggregation.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace ApiAggregation.UnitTests;

public class AccountControllerTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly AccountController _controller;
    private readonly AuthRequest _defaultUser;
    private readonly RefreshTokenRequest _defaultRefreshRequest;
    

    public AccountControllerTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _controller = new AccountController(_mockAccountService.Object);
        _defaultUser = new AuthRequest("testuser", "password");
        _defaultRefreshRequest = new RefreshTokenRequest("sample-refresh-token");
    }

    [Fact]
    public void Register_ReturnsOk_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var authResponse = new AuthResponse(true, "User registered successfully.", null, null);

        _mockAccountService
            .Setup(s => s.Register(_defaultUser))
            .Returns(authResponse);

        // Act
        var result = _controller.Register(_defaultUser);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains(authResponse.Message, okResult.Value.ToString());
    }

    [Fact]
    public void Register_ReturnsBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var authResponse = new AuthResponse(false, "Registration failed.", null, null);

        _mockAccountService
            .Setup(s => s.Register(_defaultUser))
            .Returns(authResponse);

        // Act
        var result = _controller.Register(_defaultUser);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Registration failed.", badRequestResult.Value.ToString());
    }

    [Fact]
    public void Login_ReturnsOk_WhenLoginIsSuccessful()
    {
        // Arrange
        var authResponse = new AuthResponse(true, "Login successful.", null, null);

        _mockAccountService
            .Setup(s => s.Login(_defaultUser))
            .Returns(authResponse);

        // Act
        var result = _controller.Login(_defaultUser);

        // Assert - using Shouldly
        var okResult = result as OkObjectResult;
        okResult.ShouldNotBeNull();
        var value = okResult.Value as AuthResponse;
        value.ShouldBe(authResponse);
    }

    [Fact]
    public void Login_ReturnsBadRequest_WhenLoginFails()
    {
        // Arrange
        var authResponse = new AuthResponse(false, "Invalid credentials.", null, null);

        _mockAccountService
            .Setup(s => s.Login(_defaultUser))
            .Returns(authResponse);

        // Act
        var result = _controller.Login(_defaultUser);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid credentials.", badRequestResult.Value.ToString());
    }

    [Fact]
    public void Refresh_ReturnsOk_WhenRefreshTokenIsSuccessful()
    {
        // Arrange
        var authResponse = new AuthResponse(true, "Token refreshed.", null, null);

        _mockAccountService
            .Setup(s => s.RefreshToken(_defaultRefreshRequest.RefreshToken))
            .Returns(authResponse);

        // Act
        var result = _controller.Refresh(_defaultRefreshRequest);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.ShouldNotBeNull();
        var value = okResult.Value as AuthResponse;
        value.ShouldBe(authResponse);
    }

    [Fact]
    public void Refresh_ReturnsBadRequest_WhenRefreshTokenFails()
    {
        // Arrange
        var authResponse = new AuthResponse(false, "Invalid refresh token.", null, null);

        _mockAccountService
            .Setup(s => s.RefreshToken(_defaultRefreshRequest.RefreshToken))
            .Returns(authResponse);

        // Act
        var result = _controller.Refresh(_defaultRefreshRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid refresh token.", badRequestResult.Value.ToString());
    }
}
