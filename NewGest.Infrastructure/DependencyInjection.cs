using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;
using NewGest.Domain.Entities.Users;
using NewGest.Infrastructure.Auth;
using NewGest.Infrastructure.Data;
using NewGest.Infrastructure.Data.Repositories;
using NewGest.Infrastructure.Services;

namespace NewGest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<NewgestDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("NewgestDb"),
                sql => sql.MigrationsAssembly(typeof(NewgestDbContext).Assembly.FullName)));

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Política de contraseñas mínima (en línea con validador FluentValidation)
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;

            // Lockout lo manejamos manualmente para poder devolver mensaje específico
            options.Lockout.AllowedForNewUsers = false;

            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<NewgestDbContext>()
        .AddDefaultTokenProviders();

        // Repositorios — Sprint 1-4 (catálogo)
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IParametroRepository, ParametroRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IArticuloRepository, ArticuloRepository>();
        services.AddScoped<IGrupoArticuloRepository, GrupoArticuloRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositorios — Sprint 5-7 (inventario, pedidos, personal)
        services.AddScoped<IDepositoRepository, DepositoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IViajeRepository, ViajeRepository>();
        services.AddScoped<IMutualRepository, MutualRepository>();

        // Repositorios — Sprint 8-11 (facturación)
        services.AddScoped<IComprobanteRepository, ComprobanteRepository>();

        // Repositorios — Sprint 14-15 (contabilidad)
        services.AddScoped<ICuentaContableRepository, CuentaContableRepository>();
        services.AddScoped<IAsientoRepository, AsientoRepository>();

        // Servicios de infraestructura
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IAfipService, AfipServiceVfpWrapper>();
        services.AddScoped<IQrFiscalService, QrFiscalService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Servicios de reportes — Sprint 16-18
        services.AddScoped<IReportService, Reports.ReportService>();
        services.AddScoped<IExcelExportService, Reports.ExcelExportService>();

        // Servicios de integraciones — Sprint 19-20
        services.AddScoped<IEmailService, Email.MailKitEmailService>();
        services.AddScoped<IWhatsAppService, Email.WhatsAppService>();
        services.AddScoped<IWordExportService, Email.WordExportService>();
        services.AddHttpClient("WhatsApp");

        return services;
    }
}
