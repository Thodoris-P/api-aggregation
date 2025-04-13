using ApiAggregation.Authentication;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace ApiAggregation.UnitTests;

public class AccountControllerTests
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly AccountController _controller;
    private readonly UserLogin _defaultUser;
    private readonly RefreshTokenRequest _defaultRefreshRequest;

    public AccountControllerTests()
    {
        _mockAccountService = new Mock<IAccountService>();
        _controller = new AccountController(_mockAccountService.Object);
        _defaultUser = new UserLogin("testuser", "password");
        _defaultRefreshRequest = new RefreshTokenRequest("sample-refresh-token");
    }

    [Fact]
    public void Register_ReturnsOk_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var authResponse = new AuthResponse
        {
            IsSuccessful = true,
            Message = "User registered successfully."
        };

        _mockAccountService
            .Setup(s => s.Register(_defaultUser.Username, _defaultUser.Password))
            .Returns(authResponse);

        // Act
        var result = _controller.Register(_defaultUser);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("User registered successfully.", okResult.Value.ToString());
    }

    [Fact]
    public void Register_ReturnsBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var authResponse = new AuthResponse
        {
            IsSuccessful = false,
            Message = "Registration failed."
        };

        _mockAccountService
            .Setup(s => s.Register(_defaultUser.Username, _defaultUser.Password))
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
        var authResponse = new AuthResponse
        {
            IsSuccessful = true,
            Message = "Login successful."
        };

        _mockAccountService
            .Setup(s => s.Login(_defaultUser.Username, _defaultUser.Password))
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
        var authResponse = new AuthResponse
        {
            IsSuccessful = false,
            Message = "Invalid credentials."
        };

        _mockAccountService
            .Setup(s => s.Login(_defaultUser.Username, _defaultUser.Password))
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
        var authResponse = new AuthResponse
        {
            IsSuccessful = true,
            Message = "Token refreshed."
        };

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
        var authResponse = new AuthResponse
        {
            IsSuccessful = false,
            Message = "Invalid refresh token."
        };

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
