namespace NHS111.Application.DTOs.Requests;

public record SignupRequest(
    string Username,
    string Email,
    string Password,
    string? Role = "User");
