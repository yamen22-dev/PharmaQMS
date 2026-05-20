using System.Collections.Generic;
using PharmaQMS.API.DTOs.RawMaterials;

namespace PharmaQMS.API.Services;

public interface IRawMaterialService
{
    Task<IReadOnlyList<RawMaterialOverviewResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RawMaterialDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RawMaterialResponse> CreateAsync(CreateRawMaterialRequest request, CancellationToken cancellationToken = default);
}