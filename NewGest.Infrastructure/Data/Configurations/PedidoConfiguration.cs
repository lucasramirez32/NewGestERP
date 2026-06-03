using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos", schema: "com");
        builder.HasKey(p => p.IdPedido);
        builder.Property(p => p.IdPedido).UseIdentityColumn();
        builder.Property(p => p.IdEmpresa).IsRequired();
        builder.Property(p => p.IdCliente).IsRequired();
        builder.Property(p => p.Estado).HasConversion<int>().IsRequired();
        builder.Property(p => p.FechaPedido).IsRequired();
        builder.Property(p => p.Observaciones).HasColumnType("NVARCHAR(MAX)");

        builder.HasMany(p => p.Items)
               .WithOne()
               .HasForeignKey(i => i.IdPedido)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.IdEmpresa, p.Estado });
        builder.HasIndex(p => new { p.IdEmpresa, p.IdCliente });
    }
}

public class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItemsPedido", schema: "com");
        builder.HasKey(i => i.IdItemPedido);
        builder.Property(i => i.IdItemPedido).UseIdentityColumn();
        builder.Property(i => i.IdPedido).IsRequired();
        builder.Property(i => i.IdArticulo).IsRequired();
        builder.Property(i => i.CantidadPedida).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(i => i.CantidadEntregada).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(i => i.PrecioUnitario).HasColumnType("DECIMAL(18,4)").IsRequired();

        // CantidadPendiente es computed en .NET — no mapeada a columna
        builder.Ignore(i => i.CantidadPendiente);
    }
}
