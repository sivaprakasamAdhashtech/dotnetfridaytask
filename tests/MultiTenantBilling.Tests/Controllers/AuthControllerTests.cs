using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MultiTenantBilling.Api.Controllers;
using MultiTenantBilling.Core.DTOs;
using MultiTenantBilling.Core.Exceptions;
using MultiTenantBilling.Core.Interfaces;
using Xunit;

namespace MultiTenantBilling.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _auditLogServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var loginResponse = new LoginResponse
        {
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = "user-id",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                TenantId = "TENANT-001",
                Role = "TenantAdmin",
                IsActive = true,
                FullName = "Test User"
            }
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(loginResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().Be("jwt-token");
        apiResponse.Data.User.Email.Should().Be("test@example.com");

        _authServiceMock.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest))
            .ThrowsAsync(new UnauthorizedException("Invalid email or password."));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<LoginResponse>>().Subject;
        
        apiResponse.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain("Invalid email or password.");

        _authServiceMock.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk()
    {
        // Arrange
        _controller.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        _controller.HttpContext.Items["UserId"] = "user-id";

        _authServiceMock.Setup(x => x.LogoutAsync("user-id"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Logout successful");

        _authServiceMock.Verify(x => x.LogoutAsync("user-id"), Times.Once);
    }
}
