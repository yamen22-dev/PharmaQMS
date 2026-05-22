using Microsoft.EntityFrameworkCore;
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
    }
}