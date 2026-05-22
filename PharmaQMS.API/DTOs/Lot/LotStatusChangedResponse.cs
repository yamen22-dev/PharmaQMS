namespace PharmaQMS.API.DTOs.Lot;

public sealed record LotStatusChangedResponse(
    int Id,
    string LotNumber,
    string OldStatus,
    string NewStatus
);