using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.DTOs.QualityControl;

public sealed record QcTestListQuery
{
    public QcTestStatus? Status { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}