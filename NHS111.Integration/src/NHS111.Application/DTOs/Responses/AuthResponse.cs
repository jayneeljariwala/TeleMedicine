namespace NHS111.Application.DTOs.Responses;

public record AuthResponse(
    string Token,
    string TokenType,
    string Username,
    string Email,
    string Role,
    DateTime ExpiresAt);
