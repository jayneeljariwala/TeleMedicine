namespace NHS111.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NHS111.API.Controllers;
using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Services;
using Xunit;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Signup_CallsAuthService_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new SignupRequest("alice", "alice@example.com", "Password123!");
        var expectedResponse = new AuthResponse("token-123", "Bearer", "alice", "alice@example.com", "User", DateTime.UtcNow.AddHours(1));

        _authServiceMock.Setup(s => s.SignupAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Signup(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Login_CallsAuthService_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest("alice", "Password123!");
        var expectedResponse = new AuthResponse("jwt-token-xyz", "Bearer", "alice", "alice@example.com", "User", DateTime.UtcNow.AddHours(1));

        _authServiceMock.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Logout_ExtractsHeaderAndCallsAuthService_ReturnsOk()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer my-jwt-token";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.Logout(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _authServiceMock.Verify(s => s.LogoutAsync("Bearer my-jwt-token", It.IsAny<CancellationToken>()), Times.Once);
    }
}
