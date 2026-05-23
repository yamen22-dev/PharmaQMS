using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Data.Configurations;

internal sealed class QcTestConfiguration : IEntityTypeConfiguration<QcTest>
{
    public void Configure(EntityTypeBuilder<QcTest> b)
    {
        b.ToTable("QcTests");
        b.HasKey(t => t.Id);

        b.Property(t => t.TestObjectType)
            .HasMaxLength(20)
            .IsRequired();

        b.Property(t => t.Status)
            .HasConversion<byte>()
            .IsRequired();

        b.Property(t => t.CreatedBy)
            .HasMaxLength(450)
            .IsRequired();

        b.HasMany(t => t.Parameters)
            .WithOne(p => p.QcTest)
            .HasForeignKey(p => p.QcTestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.Status);
        b.HasIndex(t => t.CreatedAt);
    }
}