using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Configurations;

public class RemitoConfiguration : IEntityTypeConfiguration<Remito>
{
    public void Configure(EntityTypeBuilder<Remito> builder)
    {
        builder.ToTable("Remitos", schema: "com");
        builder.HasKey(r => r.IdRemito);
        builder.Property(r => r.IdRemito).UseIdentityColumn();
        builder.Property(r => r.IdEmpresa).IsRequired();
        builder.Property(r => r.IdPedido).IsRequired();
        builder.Property(r => r.IdCliente).IsRequired();
        builder.Property(r => r.FechaRemito).IsRequired();
        builder.Property(r => r.Observaciones).HasColumnType("NVARCHAR(MAX)");

        builder.HasMany(r => r.Items)
               .WithOne()
               .HasForeignKey(i => i.IdRemito)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.IdEmpresa, r.IdPedido });
    }
}

public class ItemRemitoConfiguration : IEntityTypeConfiguration<ItemRemito>
{
    public void Configure(EntityTypeBuilder<ItemRemito> builder)
    {
        builder.ToTable("ItemsRemito", schema: "com");
        builder.HasKey(i => i.IdItemRemito);
        builder.Property(i => i.IdItemRemito).UseIdentityColumn();
        builder.Property(i => i.IdRemito).IsRequired();
        builder.Property(i => i.IdArticulo).IsRequired();
        builder.Property(i => i.Cantidad).HasColumnType("DECIMAL(18,4)").IsRequired();
        builder.Property(i => i.PrecioUnitario).HasColumnType("DECIMAL(18,4)").IsRequired();
    }
}
