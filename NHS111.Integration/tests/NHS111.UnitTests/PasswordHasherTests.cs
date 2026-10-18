namespace NHS111.UnitTests;

using FluentAssertions;
using NHS111.Application.Services;
using Xunit;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_GeneratesValidHashAndSalt()
    {
        // Act
        var hash = _hasher.HashPassword("MySecretPass123!");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Split('.').Length.Should().Be(3);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePassword@2024";
        var hash = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePassword@2024";
        var hash = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword("IncorrectPassword", hash);

        // Assert
        isValid.Should().BeFalse();
    }
}
