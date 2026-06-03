using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Infrastructure.Data.Configurations;

public class ExistenciaDepositoConfiguration : IEntityTypeConfiguration<ExistenciaDeposito>
{
    public void Configure(EntityTypeBuilder<ExistenciaDeposito> builder)
    {
        builder.ToTable("ExistenciasDeposito", schema: "inv");
        builder.HasKey(e => e.IdExistencia);
        builder.Property(e => e.IdExistencia).UseIdentityColumn();
        builder.Property(e => e.IdEmpresa).IsRequired();
        builder.Property(e => e.IdArticulo).IsRequired();
        builder.Property(e => e.IdDeposito).IsRequired();
        builder.Property(e => e.Cantidad).HasColumnType("DECIMAL(18,4)");
        builder.Property(e => e.CostoPromedio).HasColumnType("DECIMAL(18,4)");
        builder.Property(e => e.StockMinimo).HasColumnType("DECIMAL(18,4)");

        builder.HasIndex(e => new { e.IdArticulo, e.IdDeposito }).IsUnique();
        builder.HasIndex(e => new { e.IdEmpresa, e.IdArticulo });
    }
}
