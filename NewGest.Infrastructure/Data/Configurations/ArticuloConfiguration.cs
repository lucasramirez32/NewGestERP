using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.ToTable("Articulos", schema: "neg");
        builder.HasKey(a => a.IdArticulo);
        builder.Property(a => a.IdArticulo).UseIdentityColumn();
        builder.Property(a => a.IdEmpresa).IsRequired();
        builder.Property(a => a.Codigo).IsRequired().HasColumnType("CHAR(15)");
        builder.Property(a => a.Descripcion).IsRequired().HasMaxLength(100);
        builder.Property(a => a.PrecioLista).HasColumnType("DECIMAL(18,4)");
        builder.Property(a => a.PrecioCosto).HasColumnType("DECIMAL(18,4)");
        builder.Property(a => a.PorcentajeIva).HasColumnType("DECIMAL(5,2)");
        builder.Property(a => a.Observaciones).HasColumnType("NVARCHAR(MAX)");
        builder.Property(a => a.Activo).IsRequired();

        builder.HasIndex(a => new { a.IdEmpresa, a.Codigo }).IsUnique();

        // FK a GrupoArticulo
        builder.HasOne(a => a.Grupo)
            .WithMany(g => g.Articulos)
            .HasForeignKey(a => a.IdGrupo)
            .OnDelete(DeleteBehavior.Restrict);

        // FK a Unidad
        builder.HasOne(a => a.Unidad)
            .WithMany()
            .HasForeignKey(a => a.IdUnidad)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete: filtro global
        builder.HasQueryFilter(a => a.Activo);
    }
}
