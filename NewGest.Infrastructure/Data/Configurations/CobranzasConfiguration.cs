using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Configurations;

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos", schema: "com");
        builder.HasKey(p => p.IdPago);
        builder.Property(p => p.IdPago).UseIdentityColumn();
        builder.Property(p => p.IdEmpresa).IsRequired();
        builder.Property(p => p.IdCliente).IsRequired();
        builder.Property(p => p.Numero).IsRequired();
        builder.Property(p => p.Fecha).IsRequired();
        builder.Property(p => p.TotalMedios).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(p => p.TotalImputado).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(p => p.SaldoAFavor).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(p => p.Observaciones).HasMaxLength(500);
        builder.Property(p => p.Anulado).IsRequired();

        builder.HasMany(p => p.Medios)
               .WithOne().HasForeignKey("IdPago").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Imputaciones)
               .WithOne().HasForeignKey(i => i.IdPago).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Retenciones)
               .WithOne().HasForeignKey("IdPago").OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.IdEmpresa, p.Numero }).IsUnique();
        builder.HasIndex(p => new { p.IdEmpresa, p.IdCliente });
    }
}

public class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("MediosPago", schema: "com");
        builder.HasKey(m => m.IdMedioPago);
        builder.Property(m => m.IdMedioPago).UseIdentityColumn();
        builder.Property(m => m.Tipo).HasConversion<int>().IsRequired();
        builder.Property(m => m.Monto).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(m => m.BancoEmisor).HasMaxLength(100);
        builder.Property(m => m.NumeroCheque).HasMaxLength(20);
        builder.Property(m => m.NumeroTransferencia).HasMaxLength(50);
    }
}

public class ImputacionConfiguration : IEntityTypeConfiguration<Imputacion>
{
    public void Configure(EntityTypeBuilder<Imputacion> builder)
    {
        builder.ToTable("Imputaciones", schema: "com");
        builder.HasKey(i => i.IdImputacion);
        builder.Property(i => i.IdImputacion).UseIdentityColumn();
        builder.Property(i => i.IdPago).IsRequired();
        builder.Property(i => i.IdComprobante).IsRequired();
        builder.Property(i => i.Monto).HasColumnType("DECIMAL(18,2)").IsRequired();

        builder.HasIndex(i => new { i.IdPago, i.IdComprobante });
    }
}

public class RetencionConfiguration : IEntityTypeConfiguration<Retencion>
{
    public void Configure(EntityTypeBuilder<Retencion> builder)
    {
        builder.ToTable("Retenciones", schema: "com");
        builder.HasKey(r => r.IdRetencion);
        builder.Property(r => r.IdRetencion).UseIdentityColumn();
        builder.Property(r => r.IdEmpresa).IsRequired();
        builder.Property(r => r.Tipo).HasConversion<int>().IsRequired();
        builder.Property(r => r.Provincia).HasMaxLength(3);
        builder.Property(r => r.Porcentaje).HasColumnType("DECIMAL(5,2)").IsRequired();
        builder.Property(r => r.BaseImponible).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(r => r.MontoRetenido).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(r => r.NumeroFormulario).HasMaxLength(50).IsRequired();
    }
}
