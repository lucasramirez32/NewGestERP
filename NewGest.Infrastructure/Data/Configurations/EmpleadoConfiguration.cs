using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Infrastructure.Data.Configurations;

public class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        builder.ToTable("Empleados", schema: "neg");
        builder.HasKey(e => e.IdEmpleado);
        builder.Property(e => e.IdEmpleado).UseIdentityColumn();
        builder.Property(e => e.IdEmpresa).IsRequired();
        builder.Property(e => e.Legajo).IsRequired().HasColumnType("CHAR(10)");
        builder.Property(e => e.ApellidoNombre).IsRequired().HasMaxLength(100);
        builder.Property(e => e.CUIL).HasColumnType("CHAR(11)");
        builder.Property(e => e.Rol).HasConversion<int>().IsRequired();
        builder.Property(e => e.EsVendedor).IsRequired();
        builder.Property(e => e.ComisionPorcentaje).HasColumnType("DECIMAL(5,2)");
        builder.Property(e => e.Activo).IsRequired();

        builder.HasIndex(e => new { e.IdEmpresa, e.Legajo }).IsUnique();
        builder.HasIndex(e => new { e.IdEmpresa, e.EsVendedor });

        // Trigger de auditoría incompatible con OUTPUT clause
        builder.ToTable(t => t.UseSqlOutputClause(false));

        builder.HasQueryFilter(e => e.Activo);
    }
}
