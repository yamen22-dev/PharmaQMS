using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data.Configurations;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Data;

public class DomainDbContext : DbContext
{
    public DomainDbContext(DbContextOptions<DomainDbContext> options)
        : base(options)
    {
    }

    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Bmr> Bmrs => Set<Bmr>();

    public DbSet<BmrStep> BmrSteps => Set<BmrStep>();

    public DbSet<BmrLotLink> BmrLotLinks => Set<BmrLotLink>();

    public DbSet<QcTest> QcTests => Set<QcTest>();

    public DbSet<QcTestParameter> QcTestParameters => Set<QcTestParameter>();

    public DbSet<MasterRecipe> MasterRecipes => Set<MasterRecipe>();

    public DbSet<MasterRecipeStep> MasterRecipeSteps => Set<MasterRecipeStep>();

    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RawMaterial>(entity =>
        {
            entity.ToTable("RawMaterials");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PharmaceuticalApi).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Supplier).HasMaxLength(200);
            entity.Property(x => x.CepNumber).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.MinSpecificationLimit).HasPrecision(18, 4);
            entity.Property(x => x.MaxSpecificationLimit).HasPrecision(18, 4);
            entity.Property(x => x.Category).HasConversion<byte>();

            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.PharmaceuticalApi);
            entity.HasIndex(x => x.CepNumber);
        });

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.ToTable("Lots");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LotNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 4);
            entity.Property(x => x.Status).HasConversion<byte>();
            entity.Property(x => x.ExpiryDateUtc).IsRequired();
            entity.Property(x => x.PurchaseOrderNumber).HasMaxLength(100);
            entity.Property(x => x.AnalysisCertificate).HasMaxLength(100);

            entity.HasIndex(x => x.RawMaterialId);
            entity.HasIndex(x => new { x.RawMaterialId, x.LotNumber }).IsUnique();
            entity.HasIndex(x => x.ReceivedDateUtc);
            entity.HasIndex(x => x.Status);
            
            entity.HasOne(x => x.RawMaterial)
                .WithMany(x => x.Lots)
                .HasForeignKey(x => x.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLog");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.GebruikerId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Actie).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EntiteitType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EntiteitId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.IPAdres).HasMaxLength(100);

            entity.HasIndex(x => x.GebruikerId);
            entity.HasIndex(x => x.EntiteitType);
            entity.HasIndex(x => x.Tijdstip);
            entity.HasIndex(x => new { x.EntiteitType, x.EntiteitId });
        });

        modelBuilder.Entity<Bmr>(entity =>
        {
            entity.ToTable("Bmrs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.BatchSize).HasPrecision(18, 4);
            entity.Property(x => x.Status).HasConversion<byte>();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.CreatedById).HasMaxLength(450).IsRequired();

            entity.HasIndex(x => x.BatchNumber);
            entity.HasIndex(x => x.MasterRecipeId);
            entity.HasIndex(x => x.ProductionLineId);
            entity.HasIndex(x => x.CreatedById);

            entity.HasOne(x => x.MasterRecipe)
                .WithMany()
                .HasForeignKey(x => x.MasterRecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ProductionLine)
                .WithMany()
                .HasForeignKey(x => x.ProductionLineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BmrStep>(entity =>
        {
            entity.ToTable("BmrSteps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<byte>();
            entity.Property(x => x.EnteredData).HasMaxLength(1000);
            entity.Property(x => x.EnteredById).HasMaxLength(450);
            entity.Property(x => x.EnteredAt);
            entity.Property(x => x.VerifiedById).HasMaxLength(450);
            entity.Property(x => x.VerifiedAt);
            entity.Property(x => x.DeviationNote).HasMaxLength(1000);

            entity.HasIndex(x => x.BmrId);
            entity.HasIndex(x => x.MasterRecipeStepId);
            entity.HasIndex(x => x.EnteredById);
            entity.HasIndex(x => x.VerifiedById);

            entity.HasOne(x => x.Bmr)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.BmrId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.MasterRecipeStep)
                .WithMany()
                .HasForeignKey(x => x.MasterRecipeStepId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BmrLotLink>(entity =>
        {
            entity.ToTable("BmrLotLinks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BmrId).IsRequired();
            entity.Property(x => x.LotId).IsRequired();

            entity.HasIndex(x => x.BmrId);
            entity.HasIndex(x => x.LotId);
            entity.HasIndex(x => new { x.BmrId, x.LotId }).IsUnique();

            entity.HasOne(x => x.Bmr)
                .WithMany()
                .HasForeignKey(x => x.BmrId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Lot)
                .WithMany()
                .HasForeignKey(x => x.LotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MasterRecipe>(entity =>
        {
            entity.ToTable("MasterRecipes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecipeName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Version).HasMaxLength(50).IsRequired();
            entity.Property(x => x.IsApproved).IsRequired();

            entity.HasIndex(x => x.RecipeName);
            entity.HasData(
                new MasterRecipe { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), RecipeName = "AMOX-500MG-CAP", Version = "3.1", IsApproved = true },
                new MasterRecipe { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), RecipeName = "AMOX-250MG-CAP", Version = "2.0", IsApproved = true }
            );

        });

        modelBuilder.Entity<MasterRecipeStep>(entity =>
        {
            entity.ToTable("MasterRecipeSteps");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MasterRecipeId).IsRequired();
            entity.Property(x => x.StepNumber).IsRequired();
            entity.Property(x => x.StepName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IsCritical).IsRequired();
            entity.Property(x => x.ExpectedFields).HasMaxLength(2000);

            entity.HasIndex(x => x.MasterRecipeId);
            entity.HasIndex(x => new { x.MasterRecipeId, x.StepNumber }).IsUnique();

            entity.HasOne(x => x.MasterRecipe)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.MasterRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000001"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 1, StepName = "Grondstoffen wegen", IsCritical = true, ExpectedFields = "Gewicht (kg)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 2, StepName = "Mengen", IsCritical = false, ExpectedFields = "Mengtijd (min)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 3, StepName = "Granuleren", IsCritical = true, ExpectedFields = "Temperatuur (°C); Tijd (min)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000004"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 4, StepName = "Drogen", IsCritical = false, ExpectedFields = "Vochtigheid (%)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000005"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 5, StepName = "Capsules vullen", IsCritical = true, ExpectedFields = "Vulgewicht (mg)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000006"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000001"), StepNumber = 6, StepName = "In-process kwaliteitscheck", IsCritical = true, ExpectedFields = "Resultaat" },

                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000007"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000002"), StepNumber = 1, StepName = "Grondstoffen wegen", IsCritical = true, ExpectedFields = "Gewicht (kg)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000008"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000002"), StepNumber = 2, StepName = "Mengen", IsCritical = false, ExpectedFields = "Mengtijd (min)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000009"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000002"), StepNumber = 3, StepName = "Capsules vullen", IsCritical = true, ExpectedFields = "Vulgewicht (mg)" },
                new MasterRecipeStep { Id = Guid.Parse("33333333-0000-0000-0000-000000000010"), MasterRecipeId = Guid.Parse("22222222-0000-0000-0000-000000000002"), StepNumber = 4, StepName = "In-process kwaliteitscheck", IsCritical = true, ExpectedFields = "Resultaat" }
            );
        });

        modelBuilder.Entity<ProductionLine>(entity =>
        {
            entity.ToTable("ProductionLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LineName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();

            entity.HasIndex(x => x.LineName);
            entity.HasData(
                new ProductionLine { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), LineName = "Lijn A", Location = "Hal 1", IsActive = true },
                new ProductionLine { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), LineName = "Lijn B", Location = "Hal 1", IsActive = true },
                new ProductionLine { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), LineName = "Lijn C", Location = "Hal 2", IsActive = true }
            );
        });

        modelBuilder.ApplyConfiguration(new QcTestConfiguration());
        modelBuilder.ApplyConfiguration(new QcTestParameterConfiguration());
    }

}