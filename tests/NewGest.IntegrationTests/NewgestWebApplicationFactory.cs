using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Domain.Entities.Empresas;
using NewGest.Domain.Entities.Users;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests;

/// <summary>
/// Factory que inicializa la app apuntando a NewGest_Test.
/// Limpia y reseedea la BD antes de cada colección de tests.
/// </summary>
public class NewgestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ConnectionString =
        "Server=.\\SQLEXPRESS;Database=NewGest_Test;Trusted_Connection=True;TrustServerCertificate=True";

    // Datos de la empresa de prueba
    public const int IdEmpresaTest = 1;
    public const string NombreEmpresaTest = "Empresa Test";

    // Datos del usuario admin de prueba
    public const string UsuarioAdmin = "admin";
    public const string PasswordAdmin = "Admin1234";
    public const string NombreAdmin = "Administrador Test";

    // Datos del usuario con DebeResetearPassword = true
    public const string UsuarioPrimerLogin = "primervez";
    public const string PasswordPrimerLogin = "Primer1234";
    public const string NombrePrimerLogin = "Usuario Primer Login";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            // Sobrescribir connection string para apuntar a NewGest_Test.
            // El Jwt:Secret debe ser idéntico al de appsettings.json para que la validación
            // del token use el mismo secret con el que fue firmado.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NewgestDb"] = ConnectionString,
                ["Jwt:Secret"] = "CAMBIAR_EN_PRODUCCION_minimo32caracteres_secreto",
                ["Jwt:Issuer"] = "newgest-api",
                ["Jwt:Audience"] = "newgest-web",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover DbContext registrado (apunta a NewGest producción)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NewgestDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Re-registrar apuntando a NewGest_Test
            services.AddDbContext<NewgestDbContext>(options =>
                options.UseSqlServer(ConnectionString));
        });
    }

    // IAsyncLifetime: se ejecuta una vez antes de que xUnit corra cualquier test de la colección
    public async Task InitializeAsync() => await InicializarBdAsync();

    public new Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Asegura que la BD de tests está migrada y contiene datos de prueba base.
    /// Limpia y reseedea completamente — idempotente.
    /// </summary>
    public async Task InicializarBdAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Migrar si hay pendientes
        await db.Database.MigrateAsync();

        // Limpiar datos de pruebas anteriores (orden inverso por FK)
        db.Parametros.RemoveRange(db.Parametros);
        db.Empresas.RemoveRange(db.Empresas);
        var users = db.Users.ToList();
        foreach (var u in users)
            await userManager.DeleteAsync(u);
        await db.SaveChangesAsync();

        // Reiniciar IDENTITY para que IdEmpresa sea siempre 1 en los tests
        await db.Database.ExecuteSqlRawAsync(
            "DBCC CHECKIDENT('cfg.Empresas', RESEED, 0)");

        // Insertar empresa de prueba
        db.Empresas.Add(new Empresa
        {
            Nombre = NombreEmpresaTest,
            RazonSocial = "Empresa Test S.A.",
            Cuit = "30-71398850-1",
            Activa = true
        });
        await db.SaveChangesAsync();

        // Insertar usuario admin (DebeResetearPassword = false para tests normales)
        var admin = new ApplicationUser
        {
            UserName = UsuarioAdmin,
            NormalizedUserName = UsuarioAdmin.ToUpper(),
            IdEmpresa = IdEmpresaTest,
            Nombre = NombreAdmin,
            DebeResetearPassword = false,
            Bloqueado = false,
            IntentosFallidos = 0
        };
        var resultAdmin = await userManager.CreateAsync(admin, PasswordAdmin);
        if (!resultAdmin.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear usuario de prueba: {string.Join(", ", resultAdmin.Errors.Select(e => e.Description))}");

        // Insertar usuario con DebeResetearPassword = true
        var primerLogin = new ApplicationUser
        {
            UserName = UsuarioPrimerLogin,
            NormalizedUserName = UsuarioPrimerLogin.ToUpper(),
            IdEmpresa = IdEmpresaTest,
            Nombre = NombrePrimerLogin,
            DebeResetearPassword = true,
            Bloqueado = false,
            IntentosFallidos = 0
        };
        var resultPrimer = await userManager.CreateAsync(primerLogin, PasswordPrimerLogin);
        if (!resultPrimer.Succeeded)
            throw new InvalidOperationException(
                $"No se pudo crear usuario primer-login: {string.Join(", ", resultPrimer.Errors.Select(e => e.Description))}");
    }
}
