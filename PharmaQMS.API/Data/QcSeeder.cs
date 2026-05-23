using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.Enums;

namespace PharmaQMS.API.Data;

public static class QcSeeder
{
    private sealed record SeedQcTestParameter(int Id, string Name, string Unit, decimal Min, decimal Max);

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
                new(910001, "Appearance", "score", 0m, 5m),
                new(910002, "Moisture", "%", 0.0m, 2.0m)
            ]),
        new(
            900002,
            "Batch",
            500120,
            QcTestStatus.Approved,
            new DateTimeOffset(2026, 5, 20, 14, 40, 0, TimeSpan.Zero),
            "qa.manager@pharmaqms.local",
            [
                new(910003, "Assay", "%", 98.0m, 102.0m),
                new(910004, "Dissolution", "%", 80.0m, 100.0m)
            ]),
        new(
            900003,
            "Lot",
            102389,
            QcTestStatus.Rejected,
            new DateTimeOffset(2026, 5, 21, 10, 5, 0, TimeSpan.Zero),
            "production.analyst@pharmaqms.local",
            [
                new(910005, "Appearance", "score", 0m, 5m),
                new(910006, "Impurities", "%", 0.0m, 1.0m)
            ])
    ];

    public static async Task SeedSampleDataAsync(
        DomainDbContext db,
        CancellationToken cancellationToken = default)
    {
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
                });

            db.QcTestParameters.AddRange(missingParameters);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}