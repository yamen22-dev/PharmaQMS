using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.Data;

public static class QcSeeder
{
    private const string SeedLotNumber = "LOT-QC-SEED-2026-001";
    private const string SeedBatchNumber = "BATCH-QC-SEED-777";

    private sealed record SeedQcTestParameter(
        int Id,
        string Name,
        string Unit,
        decimal Min,
        decimal Max,
        decimal? MeasuredValue = null);

    private sealed record SeedQcTest(
        int Id,
        string TestObjectType,
        int TestObjectId,
        QcTestStatus Status,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        SeedQcTestParameter[] Parameters);

    private static readonly SeedQcTest[] SeedTests =
    [
        new(
            900001,
            "Lot",
            102341,
            QcTestStatus.InProgress,
            new DateTimeOffset(2026, 5, 19, 8, 15, 0, TimeSpan.Zero),
            "qc.analyst@pharmaqms.local",
            [
                new(910001, "Appearance", "score", 0m, 5m, 4.0m),
                new(910002, "Moisture", "%", 0.0m, 2.0m, 1.6m)
            ]),
        new(
            900002,
            "Batch",
            500120,
            QcTestStatus.Approved,
            new DateTimeOffset(2026, 5, 20, 14, 40, 0, TimeSpan.Zero),
            "qa.manager@pharmaqms.local",
            [
                new(910003, "Assay", "%", 98.0m, 102.0m, 100.1m),
                new(910004, "Dissolution", "%", 80.0m, 100.0m, 92.0m)
            ]),
        new(
            900003,
            "Lot",
            102389,
            QcTestStatus.Rejected,
            new DateTimeOffset(2026, 5, 21, 10, 5, 0, TimeSpan.Zero),
            "production.analyst@pharmaqms.local",
            [
                new(910005, "Appearance", "score", 0m, 5m, 4.0m),
                new(910006, "Impurities", "%", 0.0m, 1.0m, 1.4m)
            ])
    ];

    public static async Task SeedSampleDataAsync(
        DomainDbContext db,
        CancellationToken cancellationToken = default)
    {
        await EnsureSeedLotForQcCreateAsync(db, cancellationToken);
        await EnsureSeedBatchForQcCreateAsync(db, cancellationToken);

        foreach (var seedTest in SeedTests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var testExists = await db.QcTests.AnyAsync(t => t.Id == seedTest.Id, cancellationToken);
            if (!testExists)
            {
                db.QcTests.Add(new QcTest
                {
                    Id = seedTest.Id,
                    TestObjectType = seedTest.TestObjectType,
                    TestObjectId = seedTest.TestObjectId,
                    Status = seedTest.Status,
                    CreatedAt = seedTest.CreatedAt,
                    CreatedBy = seedTest.CreatedBy,
                    Parameters = seedTest.Parameters
                        .Select(parameter => new QcTestParameter
                        {
                            Id = parameter.Id,
                            Name = parameter.Name,
                            Unit = parameter.Unit,
                            Min = parameter.Min,
                            Max = parameter.Max,
                            MeasuredValue = parameter.MeasuredValue,
                        })
                        .ToList(),
                });

                continue;
            }

            var existingParameterIds = await db.QcTestParameters
                .Where(parameter => parameter.QcTestId == seedTest.Id)
                .Select(parameter => parameter.Id)
                .ToListAsync(cancellationToken);

            var missingParameters = seedTest.Parameters
                .Where(parameter => !existingParameterIds.Contains(parameter.Id))
                .Select(parameter => new QcTestParameter
                {
                    Id = parameter.Id,
                    QcTestId = seedTest.Id,
                    Name = parameter.Name,
                    Unit = parameter.Unit,
                    Min = parameter.Min,
                    Max = parameter.Max,
                    MeasuredValue = parameter.MeasuredValue,
                });

            db.QcTestParameters.AddRange(missingParameters);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureSeedLotForQcCreateAsync(
        DomainDbContext db,
        CancellationToken cancellationToken)
    {
        var existingSeedLot = await db.Lots
            .FirstOrDefaultAsync(l => l.LotNumber == SeedLotNumber, cancellationToken);

        if (existingSeedLot is not null)
        {
            if (existingSeedLot.Status != LotStatus.Quarantine)
            {
                existingSeedLot.Status = LotStatus.Quarantine;
            }

            return;
        }

        var rawMaterial = await db.RawMaterials
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (rawMaterial is null)
        {
            rawMaterial = new RawMaterial
            {
                Name = "QC Seed API",
                PharmaceuticalApi = "AMOXICILLIN",
                Category = RawMaterialCategory.ActivePharmaceuticalIngredient,
                Unit = "kg",
                MinSpecificationLimit = 98.0m,
                MaxSpecificationLimit = 102.0m,
                Supplier = "Seed Supplier",
                CepNumber = "CEP-QC-SEED",
                Notes = "Automatisch seed record voor QC-test aanmaak.",
                CreatedUtc = DateTime.UtcNow
            };

            db.RawMaterials.Add(rawMaterial);
            await db.SaveChangesAsync(cancellationToken);
        }

        db.Lots.Add(new Lot
        {
            RawMaterialId = rawMaterial.Id,
            LotNumber = SeedLotNumber,
            ReceivedDateUtc = DateTime.UtcNow.Date.AddDays(-2),
            Quantity = 150.0m,
            Status = LotStatus.Quarantine,
            ExpiryDateUtc = DateTime.UtcNow.Date.AddYears(1),
            PurchaseOrderNumber = "PO-QC-SEED-001",
            AnalysisCertificate = "COA-QC-SEED-001"
        });
    }

    private static async Task EnsureSeedBatchForQcCreateAsync(
        DomainDbContext db,
        CancellationToken cancellationToken)
    {
        var existingSeedBatch = await db.Bmrs
            .FirstOrDefaultAsync(b => b.BatchNumber == SeedBatchNumber, cancellationToken);

        if (existingSeedBatch is not null)
        {
            if (existingSeedBatch.Status != BmrStatus.InQc)
            {
                existingSeedBatch.Status = BmrStatus.InQc;
            }

            return;
        }

        var masterRecipeId = await db.MasterRecipes
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var productionLineId = await db.ProductionLines
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (masterRecipeId == Guid.Empty || productionLineId == Guid.Empty)
        {
            return;
        }

        db.Bmrs.Add(new Bmr
        {
            Id = Guid.NewGuid(),
            BatchNumber = SeedBatchNumber,
            MasterRecipeId = masterRecipeId,
            BatchSize = 500.0m,
            ProductionLineId = productionLineId,
            Status = BmrStatus.InQc,
            CreatedById = "seed-system",
            CreatedAt = DateTime.UtcNow
        });
    }
}