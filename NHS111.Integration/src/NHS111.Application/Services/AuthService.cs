namespace NHS111.Application.Services;

using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;
using NHS111.Application.Exceptions;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ITokenBlacklistService tokenBlacklistService) : IAuthService
{
    public async Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken ct = default)
    {
        var existingUserByUsername = await userRepository.GetByUsernameAsync(request.Username, ct);
        if (existingUserByUsername != null)
        {
            throw new ValidationException("Username is already taken.");
        }

        var existingUserByEmail = await userRepository.GetByEmailAsync(request.Email, ct);
        if (existingUserByEmail != null)
        {
            throw new ValidationException("Email is already registered.");
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();

        var user = new User(
            Guid.NewGuid(),
            request.Username.Trim(),
            request.Email.Trim().ToLowerInvariant(),
            passwordHash,
            role);

        await userRepository.AddAsync(user, ct);

        var (token, expiresAt) = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            token,
            "Bearer",
            user.Username,
            user.Email,
            user.Role,
            expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var identifier = request.UsernameOrEmail.Trim();
        var user = await userRepository.GetByUsernameAsync(identifier, ct)
            ?? await userRepository.GetByEmailAsync(identifier, ct);

        if (user == null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        var (token, expiresAt) = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            token,
            "Bearer",
            user.Username,
            user.Email,
            user.Role,
            expiresAt);
    }

    public async Task LogoutAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var cleanToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token["Bearer ".Length..].Trim()
            : token.Trim();

        if (!string.IsNullOrEmpty(cleanToken))
        {
            await tokenBlacklistService.BlacklistTokenAsync(cleanToken, TimeSpan.FromHours(24), ct);
        }
    }
}
