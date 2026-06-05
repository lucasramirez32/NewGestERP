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
app.MapParametrosEndpoints();
app.MapClientesEndpoints();
app.MapArticulosEndpoints();
app.MapStockEndpoints();
app.MapPedidosEndpoints();
app.MapPersonalEndpoints();
app.MapComprobantesEndpoints();
app.MapContabilidadEndpoints();

app.Run();

// Exponer Program para tests de integración
public partial class Program { }
