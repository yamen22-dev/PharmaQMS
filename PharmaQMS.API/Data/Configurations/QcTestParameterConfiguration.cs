using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Data.Configurations;

internal sealed class QcTestParameterConfiguration : IEntityTypeConfiguration<QcTestParameter>
{
    public void Configure(EntityTypeBuilder<QcTestParameter> b)
    {
        b.ToTable("QcTestParameters");
        b.HasKey(p => p.Id);

        b.Property(p => p.Name).HasMaxLength(100).IsRequired();
        b.Property(p => p.Unit).HasMaxLength(20).IsRequired();
        b.Property(p => p.Min).HasColumnType("decimal(18,4)");
        b.Property(p => p.Max).HasColumnType("decimal(18,4)");
        b.Property(p => p.MeasuredValue).HasColumnType("decimal(18,4)");
    }
}