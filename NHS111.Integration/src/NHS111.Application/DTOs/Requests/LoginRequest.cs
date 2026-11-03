namespace NHS111.Application.DTOs.Requests;

public record LoginRequest(
    string UsernameOrEmail,
    string Password);
