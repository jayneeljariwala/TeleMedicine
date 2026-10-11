namespace NHS111.Application.DTOs.Responses;

public record AuditLogResponse(
    Guid Id,
    Guid DispositionId,
    string Action,
    DateTime PerformedAt,
    string Details,
    string PerformedBy);
