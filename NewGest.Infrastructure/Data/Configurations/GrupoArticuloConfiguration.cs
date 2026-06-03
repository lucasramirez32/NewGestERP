using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class GrupoArticuloConfiguration : IEntityTypeConfiguration<GrupoArticulo>
{
    public void Configure(EntityTypeBuilder<GrupoArticulo> builder)
    {
        builder.ToTable("GruposArticulos", schema: "neg");
        builder.HasKey(g => g.IdGrupo);
        builder.Property(g => g.IdGrupo).UseIdentityColumn();
        builder.Property(g => g.IdEmpresa).IsRequired();
        builder.Property(g => g.Descripcion).IsRequired().HasMaxLength(100);

        // Árbol jerárquico: auto-referencia
        builder.HasOne(g => g.GrupoPadre)
            .WithMany(g => g.Subgrupos)
            .HasForeignKey(g => g.IdGrupoPadre)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.IdEmpresa, g.Descripcion, g.IdGrupoPadre }).IsUnique();
    }
}
