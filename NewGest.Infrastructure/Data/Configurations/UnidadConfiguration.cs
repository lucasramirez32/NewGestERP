using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class UnidadConfiguration : IEntityTypeConfiguration<Unidad>
{
    public void Configure(EntityTypeBuilder<Unidad> builder)
    {
        builder.ToTable("Unidades", schema: "neg");
        builder.HasKey(u => u.IdUnidad);
        builder.Property(u => u.IdUnidad).UseIdentityColumn();
        builder.Property(u => u.Descripcion).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Simbolo).IsRequired().HasMaxLength(10);

        builder.HasIndex(u => u.Descripcion).IsUnique();
    }
}
