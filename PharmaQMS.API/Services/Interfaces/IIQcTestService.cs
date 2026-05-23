using PharmaQMS.API.DTOs.QualityControl;
using PharmaQMS.API.Models.DTOs.Common;

namespace PharmaQMS.Application.QualityControl;

public interface IQcTestService
{
    /// <summary>
    /// UC-QC-01: Maak een nieuwe QC-test aan voor een lot of batch.
    /// Logt het aanmaakevent in de Audit Trail.
    /// </summary>
    Task<Result<QcTestSummaryResponse>> CreateAsync(
        CreateQcTestRequest request,
        string userId,
        CancellationToken ct = default);

    Task<Result<PagedResponse<QcTestSummaryResponse>>> GetAllAsync(
        QcTestListQuery query,
        CancellationToken ct = default);

    Task<Result<QcTestSummaryResponse>> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<Result<QcEligibleObjectsResponse>> GetEligibleObjectsAsync(
        CancellationToken ct = default);
}