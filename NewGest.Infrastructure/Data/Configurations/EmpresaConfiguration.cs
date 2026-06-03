using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Empresas;

namespace NewGest.Infrastructure.Data.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas", schema: "cfg");
        builder.HasKey(e => e.IdEmpresa);
        builder.Property(e => e.IdEmpresa).UseIdentityColumn();
        builder.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(e => e.RazonSocial).HasMaxLength(150);
        builder.Property(e => e.Cuit).HasColumnType("CHAR(13)");

        builder.HasIndex(e => e.Nombre).IsUnique();
    }
}
