using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.Interfaces;
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

        // Repositorios
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IParametroRepository, ParametroRepository>();

        // Servicios de infraestructura
        services.AddScoped<IJwtService, JwtService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
