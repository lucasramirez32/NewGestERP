using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class ViajeConfiguration : IEntityTypeConfiguration<Viaje>
{
    public void Configure(EntityTypeBuilder<Viaje> builder)
    {
        builder.ToTable("Viajes", schema: "neg");
        builder.HasKey(v => v.IdViaje);
        builder.Property(v => v.IdViaje).UseIdentityColumn();
        builder.Property(v => v.IdEmpresa).IsRequired();
        builder.Property(v => v.Descripcion).IsRequired().HasMaxLength(150);
        builder.Property(v => v.FechaViaje).IsRequired();
        builder.Property(v => v.Destino).HasMaxLength(100);
        builder.Property(v => v.Observaciones).HasColumnType("NVARCHAR(MAX)");
        builder.Property(v => v.Activo).IsRequired();

        builder.HasIndex(v => new { v.IdEmpresa, v.FechaViaje });

        builder.HasQueryFilter(v => v.Activo);
    }
}
