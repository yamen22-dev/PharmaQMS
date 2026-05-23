using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.DTOs.QualityControl;

public sealed record SubmitQcTestResultsResponse(
    int Id,
    string TestObjectType,
    int TestObjectId,
    string TestObjectLabel,
    QcTestStatus Status,
    string Message,
    bool CoaStarted,
    bool NotificationQueued,
    IReadOnlyList<QcTestParameterResponse> Parameters);