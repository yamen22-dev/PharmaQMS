using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.RawMaterials;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Services;

public class RawMaterialService : IRawMaterialService
{
    private readonly DomainDbContext _domainDbContext;

    public RawMaterialService(DomainDbContext domainDbContext)
    {
        _domainDbContext = domainDbContext;
    }

    public async Task<RawMaterialResponse> CreateAsync(CreateRawMaterialRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MinSpecificationLimit > request.MaxSpecificationLimit)
        {
            throw new ArgumentException("MinSpecificationLimit cannot be greater than MaxSpecificationLimit.");
        }

        if (!Enum.TryParse<RawMaterialCategory>(request.Category, true, out var category))
        {
            throw new ArgumentException("Category is invalid.");
        }

        var exists = await _domainDbContext.RawMaterials
            .AsNoTracking()
            .AnyAsync(x => x.Name == request.Name && x.PharmaceuticalApi == request.PharmaceuticalApi, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("A raw material with the same name and pharmaceutical API already exists.");
        }

        var entity = new RawMaterial
        {
            Name = request.Name.Trim(),
            PharmaceuticalApi = request.PharmaceuticalApi.Trim(),
            Category = category,
            Unit = request.Unit.Trim(),
            MinSpecificationLimit = request.MinSpecificationLimit,
            MaxSpecificationLimit = request.MaxSpecificationLimit,
            Supplier = request.Supplier.Trim(),
            CepNumber = request.CepNumber.Trim(),
            Notes = request.Notes.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        _domainDbContext.RawMaterials.Add(entity);
        await _domainDbContext.SaveChangesAsync(cancellationToken);

        return new RawMaterialResponse(
            entity.Id,
            entity.Name,
            entity.PharmaceuticalApi,
            entity.Category.ToString(),
            entity.Unit,
            entity.MinSpecificationLimit,
            entity.MaxSpecificationLimit,
            entity.Supplier,
            entity.CepNumber,
            entity.Notes,
            entity.CreatedUtc);
    }
}