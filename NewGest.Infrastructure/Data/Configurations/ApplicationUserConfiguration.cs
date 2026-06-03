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
    }
}
