using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Infrastructure.Data.Configurations;

public class DepositoConfiguration : IEntityTypeConfiguration<Deposito>
{
    public void Configure(EntityTypeBuilder<Deposito> builder)
    {
        builder.ToTable("Depositos", schema: "inv");
        builder.HasKey(d => d.IdDeposito);
        builder.Property(d => d.IdDeposito).UseIdentityColumn();
        builder.Property(d => d.IdEmpresa).IsRequired();
        builder.Property(d => d.Codigo).IsRequired().HasColumnType("CHAR(10)");
        builder.Property(d => d.Descripcion).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Activo).IsRequired();

        builder.HasIndex(d => new { d.IdEmpresa, d.Codigo }).IsUnique();

        builder.HasQueryFilter(d => d.Activo);
    }
}
