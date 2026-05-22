using PharmaQMS.API.DTOs.Lot;

namespace PharmaQMS.API.Services;

public interface ILotService
{
    /// <summary>UC-02e – Alle lots ophalen, optioneel gefilterd op status en/of grondstof.</summary>
    Task<IReadOnlyList<LotSummaryResponse>> GetLotsAsync(
        string? status,
        int? rawMaterialId,
        CancellationToken ct = default);

    /// <summary>UC-02f – Één lot ophalen op id.</summary>
    Task<LotDetailResponse?> GetLotByIdAsync(int id, CancellationToken ct = default);

    /// <summary>UC-02d – Lot aanmaken onder een grondstof.</summary>
    Task<LotDetailResponse> CreateLotAsync(
        int rawMaterialId,
        CreateLotRequest request,
        string createdByUserId,
        CancellationToken ct = default);

    /// <summary>UC-02g – Kwaliteitsstatus wijzigen.</summary>
    Task<LotStatusChangedResponse> ChangeLotStatusAsync(
        int id,
        ChangeLotStatusRequest request,
        string changedByUserId,
        CancellationToken ct = default);
}