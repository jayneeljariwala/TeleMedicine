namespace Ummanu.DummyAPI.Domain;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid DispositionId { get; set; }
    public string NhsNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string DxCode { get; set; } = string.Empty;
    public string DosCode { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public Guid? AssignedClinicianId { get; set; }
    public int WaitingRoomPosition { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

