using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class MutualConfiguration : IEntityTypeConfiguration<Mutual>
{
    public void Configure(EntityTypeBuilder<Mutual> builder)
    {
        builder.ToTable("Mutuales", schema: "neg");
        builder.HasKey(m => m.IdMutual);
        builder.Property(m => m.IdMutual).UseIdentityColumn();
        builder.Property(m => m.IdEmpresa).IsRequired();
        builder.Property(m => m.Codigo).IsRequired().HasColumnType("CHAR(10)");
        builder.Property(m => m.Descripcion).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Activo).IsRequired();

        builder.HasIndex(m => new { m.IdEmpresa, m.Codigo }).IsUnique();

        builder.HasQueryFilter(m => m.Activo);
    }
}
