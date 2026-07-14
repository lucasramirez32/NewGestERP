using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Config;

namespace NewGest.Infrastructure.Data.Configurations;

public class AlicuotaRetencionConfiguration : IEntityTypeConfiguration<AlicuotaRetencion>
{
    public void Configure(EntityTypeBuilder<AlicuotaRetencion> builder)
    {
        builder.ToTable("AlicuotasRetencion", schema: "cfg");
        builder.HasKey(a => a.IdAlicuota);
        builder.Property(a => a.IdAlicuota).UseIdentityColumn();
        builder.Property(a => a.IdEmpresa).IsRequired();
        builder.Property(a => a.TipoRetencion).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Provincia).HasMaxLength(3);
        builder.Property(a => a.Porcentaje).HasColumnType("DECIMAL(5,2)").IsRequired();
        builder.Property(a => a.VigenciaDesde).IsRequired();

        builder.HasIndex(a => new { a.IdEmpresa, a.TipoRetencion, a.Provincia, a.VigenciaDesde })
               .IsUnique();
    }
}
