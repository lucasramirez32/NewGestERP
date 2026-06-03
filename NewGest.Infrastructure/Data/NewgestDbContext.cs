using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Entities.Config;
using NewGest.Domain.Entities.Empresas;
using NewGest.Domain.Entities.Inv;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Entities.Users;

namespace NewGest.Infrastructure.Data;

public class NewgestDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int,
    IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public NewgestDbContext(DbContextOptions<NewgestDbContext> options) : base(options) { }

    // Catálogo
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Articulo> Articulos => Set<Articulo>();
    public DbSet<GrupoArticulo> GruposArticulos => Set<GrupoArticulo>();
    public DbSet<Unidad> Unidades => Set<Unidad>();

    // Inventario / Stock
    public DbSet<Deposito> Depositos => Set<Deposito>();
    public DbSet<ExistenciaDeposito> ExistenciasDeposito => Set<ExistenciaDeposito>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();

    // Comercial — Comprobantes (Facturación)
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<ItemComprobante> ItemsComprobante => Set<ItemComprobante>();

    // Comercial — Pedidos y Remitos
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItemsPedido => Set<ItemPedido>();
    public DbSet<Remito> Remitos => Set<Remito>();
    public DbSet<ItemRemito> ItemsRemito => Set<ItemRemito>();

    // Personal
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Viaje> Viajes => Set<Viaje>();
    public DbSet<Mutual> Mutuales => Set<Mutual>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Mover tablas de Identity al schema cfg
        builder.Entity<ApplicationUser>().ToTable("Users", schema: "cfg");
        builder.Entity<ApplicationRole>().ToTable("Roles", schema: "cfg");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles", schema: "cfg");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims", schema: "cfg");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins", schema: "cfg");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims", schema: "cfg");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens", schema: "cfg");

        // Aplicar todas las configuraciones del assembly automáticamente
        builder.ApplyConfigurationsFromAssembly(typeof(NewgestDbContext).Assembly);
    }
}
