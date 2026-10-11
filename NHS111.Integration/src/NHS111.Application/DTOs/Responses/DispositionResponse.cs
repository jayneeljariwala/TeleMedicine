namespace NHS111.Application.DTOs.Responses;

public record DispositionResponse(
    Guid Id,
    string NhsNumber,
    string PatientName,
    string DxCode,
    string DosCode,
    string Urgency,
    string RoutedTo,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
