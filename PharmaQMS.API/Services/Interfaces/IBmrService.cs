namespace PharmaQMS.API.Services.Interfaces;

using PharmaQMS.API.DTOs.MasterRecipe;
using PharmaQMS.API.DTOs.Productionline;
using PharmaQMS.API.Models.DTOs.Bmr;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.API.Models.Enums;

public interface IBmrService
{
    Task<BmrDetailResponse> CreateAsync(CreateBmrRequest request, string userId, CancellationToken ct = default);
    Task<PagedResponse<BmrSummaryResponse>> GetAllAsync(BmrListQuery query, CancellationToken ct = default);
    Task<BmrDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task ConfirmStepAsync(Guid bmrId, Guid stepId, ConfirmStepRequest request, string userId, CancellationToken ct = default);
    Task VerifyStepAsync(Guid bmrId, Guid stepId, VerifyStepRequest request, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductionLineResponse>> GetProductionLinesAsync(CancellationToken ct = default);
}