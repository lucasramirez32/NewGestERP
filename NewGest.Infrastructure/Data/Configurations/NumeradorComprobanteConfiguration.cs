using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Configurations;

public class NumeradorComprobanteConfiguration : IEntityTypeConfiguration<NumeradorComprobante>
{
    public void Configure(EntityTypeBuilder<NumeradorComprobante> builder)
    {
        builder.ToTable("NumeradoresComprobante", schema: "com");
        builder.HasKey(n => n.IdNumerador);
        builder.Property(n => n.IdNumerador).UseIdentityColumn();
        builder.Property(n => n.IdEmpresa).IsRequired();
        builder.Property(n => n.PuntoVenta).IsRequired();
        builder.Property(n => n.Tipo).HasConversion<int>().IsRequired();
        builder.Property(n => n.UltimoNumero).IsRequired();

        builder.HasIndex(n => new { n.IdEmpresa, n.PuntoVenta, n.Tipo }).IsUnique();
    }
}
