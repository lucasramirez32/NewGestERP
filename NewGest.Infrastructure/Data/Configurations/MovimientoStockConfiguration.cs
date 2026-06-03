using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Infrastructure.Data.Configurations;

public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
{
    public void Configure(EntityTypeBuilder<MovimientoStock> builder)
    {
        builder.ToTable("MovimientosStock", schema: "inv");
        builder.HasKey(m => m.IdMovimiento);
        builder.Property(m => m.IdMovimiento).UseIdentityColumn();
        builder.Property(m => m.IdEmpresa).IsRequired();
        builder.Property(m => m.IdArticulo).IsRequired();
        builder.Property(m => m.IdDeposito).IsRequired();
        builder.Property(m => m.Tipo).HasConversion<int>().IsRequired();
        builder.Property(m => m.Cantidad).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(m => m.CostoUnitario).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(m => m.NumeroSerie).HasMaxLength(50);
        builder.Property(m => m.FechaMovimiento).IsRequired();
        builder.Property(m => m.Observaciones).HasColumnType("NVARCHAR(MAX)");

        builder.HasIndex(m => new { m.IdArticulo, m.FechaMovimiento });
        builder.HasIndex(m => new { m.IdEmpresa, m.IdDeposito });
    }
}
