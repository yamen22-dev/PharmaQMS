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

    public async Task<IReadOnlyList<RawMaterialOverviewResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _domainDbContext.RawMaterials
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RawMaterialOverviewResponse(
                x.Id,
                x.Name,
                x.PharmaceuticalApi,
                x.Category.ToString(),
                x.Unit,
                x.Lots.Count(l => l.Status != LotStatus.Rejected),
                x.Lots.Count(l => l.Status == LotStatus.Quarantine)))
            .ToListAsync(cancellationToken);
    }

    public async Task<RawMaterialDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _domainDbContext.RawMaterials
            .AsNoTracking()
            .Include(x => x.Lots)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity is null)
            {
                return null;
            }

            var lots = entity.Lots
                .OrderBy(x => x.LotNumber)
                .Select(x => new RawMaterialLotResponse(
                    x.Id,
                    x.LotNumber,
                    x.Status.ToString()))
                .ToList();

            return new RawMaterialDetailResponse(
                entity.Id,
                entity.Name,
                entity.PharmaceuticalApi,
                entity.Category.ToString(),
                entity.Unit,
                entity.Supplier,
                entity.MinSpecificationLimit,
                entity.MaxSpecificationLimit,
                entity.Notes,
                lots);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Error retrieving raw material with ID {id}: {ex.Message}", ex);
        }
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