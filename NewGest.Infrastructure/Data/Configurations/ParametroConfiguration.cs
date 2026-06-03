using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Config;

namespace NewGest.Infrastructure.Data.Configurations;

public class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> builder)
    {
        builder.ToTable("Parametros", schema: "cfg");
        builder.HasKey(p => p.IdParametro);
        builder.Property(p => p.IdParametro).UseIdentityColumn();
        builder.Property(p => p.Clave).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Valor).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Descripcion).IsRequired().HasMaxLength(300);

        // Clave única por empresa (null = global)
        builder.HasIndex(p => new { p.Clave, p.IdEmpresa }).IsUnique();
    }
}
