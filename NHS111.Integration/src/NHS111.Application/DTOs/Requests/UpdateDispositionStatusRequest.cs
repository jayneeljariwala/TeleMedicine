namespace NHS111.Application.DTOs.Requests;

public record UpdateDispositionStatusRequest(
    string NewStatus,
    string UpdatedBy,
    string Notes);
