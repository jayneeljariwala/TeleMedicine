namespace NHS111.Application.Services;

using NHS111.Application.DTOs.Requests;
using NHS111.Application.DTOs.Responses;

public interface IAuthService
{
    Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync(string? token, CancellationToken ct = default);
}
