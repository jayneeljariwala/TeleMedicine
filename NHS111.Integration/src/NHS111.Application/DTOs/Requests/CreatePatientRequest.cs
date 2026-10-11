namespace NHS111.Application.DTOs.Requests;

public record CreatePatientRequest(
    string NhsNumber,
    string Name,
    DateTime DateOfBirth,
    string? PhoneNumber,
    string? Email);
