using PharmaQMS.API.Infrastructure;
namespace PharmaQMS.API.DTOs.Lot;

public sealed record ChangeLotStatusRequest(
    [SanitizedString] string NewStatus,
    [SanitizedString] string Reason,
    [SanitizedString] string Password
);