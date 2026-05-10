using PharmaQMS.API.DTOs.RawMaterials;

namespace PharmaQMS.API.Services;

public interface IRawMaterialService
{
    Task<RawMaterialResponse> CreateAsync(CreateRawMaterialRequest request, CancellationToken cancellationToken = default);
}