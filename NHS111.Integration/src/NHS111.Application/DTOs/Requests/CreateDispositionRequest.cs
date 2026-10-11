namespace NHS111.Application.DTOs.Requests;

public record CreateDispositionRequest(
    string NhsNumber,
    string PatientName,
    DateTime DateOfBirth,
    string DxCode,
    string DosCode,
    string Urgency);
