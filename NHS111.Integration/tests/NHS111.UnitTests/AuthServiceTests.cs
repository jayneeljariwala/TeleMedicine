namespace NHS111.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NHS111.Application.DTOs.Requests;
using NHS111.Application.Exceptions;
using NHS111.Application.Services;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;
using Xunit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();
    private readonly Mock<ITokenBlacklistService> _tokenBlacklistMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object,
            _tokenBlacklistMock.Object);
    }

    [Fact]
    public async Task SignupAsync_WhenValid_ReturnsAuthResponseWithToken()
    {
        // Arrange
        var request = new SignupRequest("john_doe", "john@example.com", "Password123!");
        _userRepoMock.Setup(r => r.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(p => p.HashPassword(request.Password))
            .Returns("hashed_secret");

        var expectedExpires = DateTime.UtcNow.AddHours(1);
        _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(("jwt-token-123", expectedExpires));

        // Act
        var result = await _sut.SignupAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("jwt-token-123");
        result.TokenType.Should().Be("Bearer");
        result.Username.Should().Be("john_doe");
        result.Email.Should().Be("john@example.com");
        result.Role.Should().Be("User");
        result.ExpiresAt.Should().Be(expectedExpires);

        _userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Username == "john_doe" && u.PasswordHash == "hashed_secret"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignupAsync_WhenUsernameTaken_ThrowsValidationException()
    {
        // Arrange
        var request = new SignupRequest("existing_user", "new@example.com", "Password123!");
        var existing = new User(Guid.NewGuid(), "existing_user", "other@example.com", "hash");
        _userRepoMock.Setup(r => r.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var act = () => _sut.SignupAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Username is already taken*");
    }

    [Fact]
    public async Task SignupAsync_WhenEmailTaken_ThrowsValidationException()
    {
        // Arrange
        var request = new SignupRequest("new_user", "taken@example.com", "Password123!");
        var existing = new User(Guid.NewGuid(), "other_user", "taken@example.com", "hash");
        _userRepoMock.Setup(r => r.GetByUsernameAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var act = () => _sut.SignupAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Email is already registered*");
    }

    [Fact]
    public async Task LoginAsync_WithValidUsernameAndPassword_ReturnsAuthResponse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "alice", "alice@example.com", "stored_hash", "Admin");
        var request = new LoginRequest("alice", "Secret123");

        _userRepoMock.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("Secret123", "stored_hash"))
            .Returns(true);

        var expires = DateTime.UtcNow.AddHours(1);
        _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(user))
            .Returns(("token-abc", expires));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("token-abc");
        result.Username.Should().Be("alice");
        result.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task LoginAsync_WithValidEmailAndPassword_ReturnsAuthResponse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "bob", "bob@example.com", "stored_hash", "User");
        var request = new LoginRequest("bob@example.com", "Secret123");

        _userRepoMock.Setup(r => r.GetByUsernameAsync("bob@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("bob@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("Secret123", "stored_hash"))
            .Returns(true);

        var expires = DateTime.UtcNow.AddHours(1);
        _jwtTokenGeneratorMock.Setup(j => j.GenerateToken(user))
            .Returns(("token-bob", expires));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("token-bob");
        result.Email.Should().Be("bob@example.com");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "alice", "alice@example.com", "stored_hash");
        var request = new LoginRequest("alice", "WrongPassword");

        _userRepoMock.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword", "stored_hash"))
            .Returns(false);

        // Act
        var act = () => _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid username/email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequest("unknown", "Password");

        _userRepoMock.Setup(r => r.GetByUsernameAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByEmailAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid username/email or password*");
    }

    [Fact]
    public async Task LogoutAsync_WithBearerToken_BlacklistsToken()
    {
        // Arrange
        var authHeader = "Bearer token-to-revoke";

        // Act
        await _sut.LogoutAsync(authHeader);

        // Assert
        _tokenBlacklistMock.Verify(b => b.BlacklistTokenAsync("token-to-revoke", It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
