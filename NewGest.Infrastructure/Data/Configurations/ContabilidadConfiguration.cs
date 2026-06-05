using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Cnt;

namespace NewGest.Infrastructure.Data.Configurations;

public class CuentaContableConfiguration : IEntityTypeConfiguration<CuentaContable>
{
    public void Configure(EntityTypeBuilder<CuentaContable> builder)
    {
        builder.ToTable("CuentasContables", schema: "cnt");
        builder.HasKey(c => c.IdCuenta);
        builder.Property(c => c.IdCuenta).UseIdentityColumn();
        builder.Property(c => c.IdEmpresa).IsRequired();
        builder.Property(c => c.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Naturaleza).HasConversion<int>().IsRequired();
        builder.Property(c => c.Tipo).HasConversion<int>().IsRequired();
        builder.Property(c => c.ImputaDirectamente).IsRequired();
        builder.Property(c => c.Activa).IsRequired();

        builder.HasIndex(c => new { c.IdEmpresa, c.Codigo }).IsUnique();
        builder.HasIndex(c => new { c.IdEmpresa, c.IdCuentaPadre });
    }
}

public class AsientoConfiguration : IEntityTypeConfiguration<Asiento>
{
    public void Configure(EntityTypeBuilder<Asiento> builder)
    {
        builder.ToTable("Asientos", schema: "cnt");
        builder.HasKey(a => a.IdAsiento);
        builder.Property(a => a.IdAsiento).UseIdentityColumn();
        builder.Property(a => a.IdEmpresa).IsRequired();
        builder.Property(a => a.Numero).IsRequired();
        builder.Property(a => a.Fecha).IsRequired();
        builder.Property(a => a.Descripcion).HasMaxLength(400).IsRequired();
        builder.Property(a => a.TipoAsiento).HasConversion<int>().IsRequired();
        builder.Property(a => a.Anulado).IsRequired();

        // Ignorar propiedades calculadas — no se persisten
        builder.Ignore(a => a.TotalDebe);
        builder.Ignore(a => a.TotalHaber);

        builder.HasMany(a => a.Partidas)
               .WithOne()
               .HasForeignKey(p => p.IdAsiento)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.IdEmpresa, a.Numero }).IsUnique();
        builder.HasIndex(a => new { a.IdEmpresa, a.Fecha });
        builder.HasIndex(a => a.IdComprobanteOrigen);
    }
}

public class PartidaAsientoConfiguration : IEntityTypeConfiguration<PartidaAsiento>
{
    public void Configure(EntityTypeBuilder<PartidaAsiento> builder)
    {
        builder.ToTable("PartidasAsiento", schema: "cnt");
        builder.HasKey(p => p.IdPartida);
        builder.Property(p => p.IdPartida).UseIdentityColumn();
        builder.Property(p => p.IdAsiento).IsRequired();
        builder.Property(p => p.IdCuenta).IsRequired();
        builder.Property(p => p.Debe).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(p => p.Haber).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(p => p.Concepto).HasMaxLength(300);

        builder.HasIndex(p => new { p.IdAsiento, p.IdCuenta });
    }
}
