namespace NHS111.IntegrationTests;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;
using Xunit;

public class AuthEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserRepository> _userRepoMock = new();

    public AuthEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IUserRepository));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => _userRepoMock.Object);
            });
        });
    }

    [Fact]
    public async Task SignupEndpoint_ReturnsOkWithJwtToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new SignupRequest("charlie", "charlie@example.com", "SecurePass123!");

        _userRepoMock.Setup(r => r.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/signup", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Response was {response.StatusCode}: {err}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrWhiteSpace();
        content.Username.Should().Be("charlie");
        content.Email.Should().Be("charlie@example.com");
    }

    [Fact]
    public async Task LoginEndpoint_WithValidCredentials_ReturnsJwtToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var passwordHasher = _factory.Services.GetRequiredService<IPasswordHasher>();
        var user = new User(Guid.NewGuid(), "david", "david@example.com", passwordHasher.HashPassword("MySecretPass!"));

        _userRepoMock.Setup(r => r.GetByUsernameAsync("david", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var loginRequest = new LoginRequest("david", "MySecretPass!");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrWhiteSpace();
        content.Username.Should().Be("david");
    }

    [Fact]
    public async Task LoginEndpoint_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        _userRepoMock.Setup(r => r.GetByUsernameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var loginRequest = new LoginRequest("nonexistent", "WrongPassword");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token-to-logout");

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
