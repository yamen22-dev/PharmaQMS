using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.DTOs.QualityControl;

public sealed record QcTestDetailResponse(
    int Id,
    string TestObjectType,
    int TestObjectId,
    string TestObjectLabel,
    QcTestStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    IReadOnlyList<QcTestParameterResponse> Parameters);