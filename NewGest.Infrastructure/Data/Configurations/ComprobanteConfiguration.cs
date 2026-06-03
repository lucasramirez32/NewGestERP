using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Configurations;

public class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> builder)
    {
        builder.ToTable("Comprobantes", schema: "com");
        builder.HasKey(c => c.IdComprobante);
        builder.Property(c => c.IdComprobante).UseIdentityColumn();
        builder.Property(c => c.IdEmpresa).IsRequired();
        builder.Property(c => c.Tipo).HasConversion<int>().IsRequired();
        builder.Property(c => c.PuntoVenta).IsRequired();
        builder.Property(c => c.Numero).IsRequired();
        builder.Property(c => c.Fecha).IsRequired();
        builder.Property(c => c.IdCliente).IsRequired();
        builder.Property(c => c.RazonSocialCliente).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CuitCliente).HasMaxLength(11);
        builder.Property(c => c.CondicionIvaReceptor).HasConversion<int>().IsRequired();
        builder.Property(c => c.TotalNeto).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(c => c.TotalIva).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(c => c.Total).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(c => c.Anulado).IsRequired();

        // CaeInfo como owned entity (columnas embebidas)
        builder.OwnsOne(c => c.Cae, cae =>
        {
            cae.Property(x => x.Codigo).HasColumnName("CaeCodigo").HasMaxLength(14);
            cae.Property(x => x.FechaVencimiento).HasColumnName("CaeFechaVencimiento");
        });

        builder.HasMany(c => c.Items)
               .WithOne()
               .HasForeignKey(i => i.IdComprobante)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.IdEmpresa, c.Tipo, c.PuntoVenta, c.Numero }).IsUnique();
        builder.HasIndex(c => new { c.IdEmpresa, c.IdCliente });
        builder.HasIndex(c => new { c.IdEmpresa, c.Fecha });
    }
}

public class ItemComprobanteConfiguration : IEntityTypeConfiguration<ItemComprobante>
{
    public void Configure(EntityTypeBuilder<ItemComprobante> builder)
    {
        builder.ToTable("ItemsComprobante", schema: "com");
        builder.HasKey(i => i.IdItemComprobante);
        builder.Property(i => i.IdItemComprobante).UseIdentityColumn();
        builder.Property(i => i.IdComprobante).IsRequired();
        builder.Property(i => i.IdArticulo).IsRequired();
        builder.Property(i => i.Descripcion).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Cantidad).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(i => i.PrecioUnitario).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(i => i.Alicuota).HasConversion<int>().IsRequired();
        builder.Property(i => i.SubtotalNeto).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(i => i.Iva).HasColumnType("DECIMAL(18,2)").IsRequired();
        builder.Property(i => i.Subtotal).HasColumnType("DECIMAL(18,2)").IsRequired();
    }
}
