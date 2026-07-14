using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes", schema: "neg");
        builder.HasKey(c => c.IdCliente);
        builder.Property(c => c.IdCliente).UseIdentityColumn();
        builder.Property(c => c.IdEmpresa).IsRequired();
        builder.Property(c => c.Codigo).IsRequired().HasColumnType("CHAR(10)");
        builder.Property(c => c.RazonSocial).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CUIT).HasColumnType("CHAR(11)");
        builder.Property(c => c.CondicionIva).HasConversion<int>();
        builder.Property(c => c.Domicilio).HasMaxLength(150);
        builder.Property(c => c.Localidad).HasMaxLength(80);
        builder.Property(c => c.Telefono).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Observaciones).HasColumnType("NVARCHAR(MAX)");
        builder.Property(c => c.Activo).IsRequired();

        // Mapeo de campos adicionales migrados de VFP
        builder.Property(c => c.NombreFantasia).HasMaxLength(100);
        builder.Property(c => c.LimiteCredito).HasColumnType("DECIMAL(18,2)").HasDefaultValue(0m);
        builder.Property(c => c.DiasMora).HasDefaultValue(0);
        builder.Property(c => c.Descuento).HasColumnType("DECIMAL(5,2)").HasDefaultValue(0m);
        builder.Property(c => c.Provincia).HasMaxLength(50);
        builder.Property(c => c.CodigoPostal).HasMaxLength(10);

        // Mapeo Ficha Médica
        builder.Property(c => c.ObraSocial).HasMaxLength(100);
        builder.Property(c => c.NroAfiliado).HasMaxLength(50);
        builder.Property(c => c.MedicoCabecera).HasMaxLength(100);
        builder.Property(c => c.MatriculaMedico).HasMaxLength(30);
        builder.Property(c => c.Alergia).HasDefaultValue(false);
        builder.Property(c => c.Alergias).HasMaxLength(200);
        builder.Property(c => c.Tratamiento).HasDefaultValue(false);
        builder.Property(c => c.Convulsiones).HasDefaultValue(false);
        builder.Property(c => c.Medicacion).HasMaxLength(200);
        builder.Property(c => c.Patologia).HasMaxLength(200);

        builder.HasIndex(c => new { c.IdEmpresa, c.Codigo }).IsUnique();
        builder.HasIndex(c => new { c.IdEmpresa, c.CUIT }).HasFilter("[CUIT] IS NOT NULL");

        // El trigger trg_Clientes_Audit en neg.Clientes es incompatible con la
        // cláusula OUTPUT que EF Core usa por defecto para recuperar el IDENTITY generado.
        // UseSqlOutputClause(false) hace que EF Core use SCOPE_IDENTITY() en su lugar.
        builder.ToTable(t => t.UseSqlOutputClause(false));

        // Soft delete: filtro global — invisible para queries normales
        builder.HasQueryFilter(c => c.Activo);
    }
}
