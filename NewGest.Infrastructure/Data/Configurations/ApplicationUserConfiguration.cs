using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewGest.Domain.Entities.Users;

namespace NewGest.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // La tabla ya fue definida en OnModelCreating (cfg.Users)
        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Legajo).HasMaxLength(20);

        builder.HasIndex(u => new { u.IdEmpresa, u.UserName }).IsUnique();

        // cfg.Users tiene un trigger de auditoría (trg_Users_Audit).
        // EF Core usa OUTPUT clause por defecto para INSERT/UPDATE, lo cual SQL Server
        // no permite en tablas con triggers habilitados (a menos que se use OUTPUT INTO).
        // UseSqlOutputClause(false) hace que EF Core use SELECT @@IDENTITY en su lugar.
        builder.ToTable(tb => tb.UseSqlOutputClause(false));
    }
}
