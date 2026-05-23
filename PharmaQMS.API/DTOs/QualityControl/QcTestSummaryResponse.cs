namespace PharmaQMS.API.DTOs.QualityControl;

using PharmaQMS.API.Models.Enums;
public sealed record QcTestSummaryResponse(
    int Id,
    string TestObjectType,
    int TestObjectId,
    QcTestStatus Status,
    DateTimeOffset CreatedAt,
    string CreatedBy
);