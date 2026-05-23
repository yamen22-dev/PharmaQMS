namespace PharmaQMS.API.Models.DTOs.Bmr;

using PharmaQMS.API.Models.Enums;

public sealed record BmrListQuery
{
    public BmrStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}