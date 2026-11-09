namespace Ummanu.DummyAPI.DTOs;

public record CreateAppointmentRequest(
    Guid DispositionId,
    string NhsNumber,
    string PatientName,
    DateTime DateOfBirth,
    string DxCode,
    string DosCode,
    string Urgency);

