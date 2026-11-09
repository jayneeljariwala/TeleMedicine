namespace Ummanu.DummyAPI.DTOs;

using Ummanu.DummyAPI.Domain;

public record AppointmentResponse(
    Guid Id,
    Guid DispositionId,
    string NhsNumber,
    string PatientName,
    DateTime DateOfBirth,
    string DxCode,
    string DosCode,
    string Urgency,
    string Status,
    Guid? AssignedClinicianId,
    int WaitingRoomPosition,
    DateTime ReceivedAt,
    DateTime UpdatedAt,
    string Notes)
{
    public static AppointmentResponse FromEntity(Appointment a) =>
        new(
            a.Id,
            a.DispositionId,
            a.NhsNumber,
            a.PatientName,
            a.DateOfBirth,
            a.DxCode,
            a.DosCode,
            a.Urgency,
            a.Status.ToString(),
            a.AssignedClinicianId,
            a.WaitingRoomPosition,
            a.ReceivedAt,
            a.UpdatedAt,
            a.Notes);
}

