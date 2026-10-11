namespace NHS111.Application.DTOs.Responses;

public record PatientResponse(
    Guid Id,
    string NhsNumber,
    string Name,
    DateTime DateOfBirth,
    string? PhoneNumber,
    string? Email);
