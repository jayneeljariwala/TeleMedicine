namespace Ummanu.DummyAPI.DTOs;

public record UpdateAppointmentStatusRequest(
    string Status,
    string? Notes = null);

