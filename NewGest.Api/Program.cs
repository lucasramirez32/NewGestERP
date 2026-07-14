using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NewGest.Api.Endpoints;
using NewGest.Api.Middleware;
using NewGest.Application;
using NewGest.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── Application + Infrastructure ───────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ─── JWT Authentication ──────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret no configurado.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// ─── Swagger con soporte JWT ─────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NewGest API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Formato: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─── CORS para el frontend en desarrollo ────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// ─── Pipeline HTTP ───────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NewGest API v1"));

    // Semilla de datos para desarrollo local
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<NewGest.Infrastructure.Data.NewgestDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<NewGest.Domain.Entities.Users.ApplicationUser>>();

        if (!db.Empresas.Any())
        {
            var empresa = new NewGest.Domain.Entities.Empresas.Empresa
            {
                Nombre = "Empresa Test",
                RazonSocial = "Empresa Test S.A.",
                Cuit = "30-71398850-1",
                Activa = true
            };
            db.Empresas.Add(empresa);
            db.SaveChanges();

            // Insertar usuario admin (DebeResetearPassword = false)
            var admin = new NewGest.Domain.Entities.Users.ApplicationUser
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                IdEmpresa = empresa.IdEmpresa,
                Nombre = "Administrador Test",
                DebeResetearPassword = false,
                Bloqueado = false,
                IntentosFallidos = 0
            };
            var resultAdmin = userManager.CreateAsync(admin, "Admin1234").GetAwaiter().GetResult();
            if (!resultAdmin.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear usuario admin semilla: {string.Join(", ", resultAdmin.Errors.Select(e => e.Description))}");
            }

            // Insertar usuario de primer login (DebeResetearPassword = true)
            var primerLogin = new NewGest.Domain.Entities.Users.ApplicationUser
            {
                UserName = "primervez",
                NormalizedUserName = "PRIMERVEZ",
                IdEmpresa = empresa.IdEmpresa,
                Nombre = "Usuario Primer Login",
                DebeResetearPassword = true,
                Bloqueado = false,
                IntentosFallidos = 0
            };
            var resultPrimer = userManager.CreateAsync(primerLogin, "Primer1234").GetAwaiter().GetResult();
            if (!resultPrimer.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear usuario primervez semilla: {string.Join(", ", resultPrimer.Errors.Select(e => e.Description))}");
            }
        }
    }
}

app.UseCors("FrontendDev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Archivos estáticos del frontend (producción: dist/ copiado a wwwroot/)
app.UseDefaultFiles();
app.UseStaticFiles();

// ─── Endpoints ───────────────────────────────────────────────────────────────
app.MapAuthEndpoints();
app.MapUsuariosEndpoints();
app.MapParametrosEndpoints();
app.MapClientesEndpoints();
app.MapArticulosEndpoints();
app.MapStockEndpoints();
app.MapPedidosEndpoints();
app.MapPersonalEndpoints();
app.MapComprobantesEndpoints();
app.MapContabilidadEndpoints();
app.MapCobranzasEndpoints();
app.MapReportesEndpoints();
app.MapNotificacionesEndpoints();

app.Run();

// Exponer Program para tests de integración
public partial class Program { }
