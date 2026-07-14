using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NewGest.Application.Common;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Users;
using NewGest.Infrastructure.Data;

namespace NewGest.Api.Endpoints;

public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios")
            .WithTags("Usuarios")
            .RequireAuthorization();

        // GET /api/usuarios
        group.MapGet("/", async (
            string? search,
            int page,
            int pageSize,
            bool soloActivos,
            ICurrentUserService currentUser,
            NewgestDbContext db,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var query = db.Users.Where(u => u.IdEmpresa == currentUser.IdEmpresa);

            if (soloActivos)
            {
                query = query.Where(u => !u.Bloqueado);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpper();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.ToUpper().Contains(s)) ||
                    u.Nombre.ToUpper().Contains(s) ||
                    (u.Legajo != null && u.Legajo.ToUpper().Contains(s)));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var dtos = new List<UsuarioDto>();
            foreach (var u in items)
            {
                var claims = await userManager.GetClaimsAsync(u);
                var permisos = claims
                    .Where(c => c.Type == NewgestClaims.Permiso)
                    .Select(c => c.Value)
                    .ToList();

                dtos.Add(new UsuarioDto(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.Nombre,
                    u.Legajo,
                    u.UltimoLogin,
                    u.Bloqueado && u.IntentosFallidos >= 5,
                    !u.Bloqueado,
                    permisos
                ));
            }

            return Results.Ok(new PagedResult<UsuarioDto>(dtos, total, page, pageSize));
        })
        .WithName("GetUsuarios")
        .Produces<PagedResult<UsuarioDto>>();

        // GET /api/usuarios/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var u = await userManager.FindByIdAsync(id.ToString());
            if (u == null || u.IdEmpresa != currentUser.IdEmpresa)
                return Results.NotFound();

            var claims = await userManager.GetClaimsAsync(u);
            var permisos = claims
                .Where(c => c.Type == NewgestClaims.Permiso)
                .Select(c => c.Value)
                .ToList();

            var dto = new UsuarioDto(
                u.Id,
                u.UserName ?? string.Empty,
                u.Nombre,
                u.Legajo,
                u.UltimoLogin,
                u.Bloqueado && u.IntentosFallidos >= 5,
                !u.Bloqueado,
                permisos
            );

            return Results.Ok(dto);
        })
        .WithName("GetUsuarioById")
        .Produces<UsuarioDto>()
        .ProducesProblem(404);

        // POST /api/usuarios
        group.MapPost("/", async (
            CrearUsuarioDto dto,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager) =>
        {
            // Validar que el usuario no exista
            var existente = await userManager.FindByNameAsync(dto.UserName);
            if (existente != null)
                return Results.BadRequest(new { message = $"Ya existe un usuario con el nombre de usuario '{dto.UserName}'." });

            var user = new ApplicationUser
            {
                IdEmpresa = currentUser.IdEmpresa,
                UserName = dto.UserName.Trim(),
                Nombre = dto.Nombre.Trim(),
                Legajo = dto.Legajo?.Trim(),
                DebeResetearPassword = true,
                Bloqueado = false,
                IntentosFallidos = 0
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            if (dto.Permisos != null && dto.Permisos.Any())
            {
                var claims = dto.Permisos.Select(p => new Claim(NewgestClaims.Permiso, p)).ToList();
                await userManager.AddClaimsAsync(user, claims);
            }

            var responseDto = new UsuarioDto(
                user.Id,
                user.UserName,
                user.Nombre,
                user.Legajo,
                user.UltimoLogin,
                false,
                true,
                dto.Permisos ?? new List<string>()
            );

            return Results.Created($"/api/usuarios/{user.Id}", responseDto);
        })
        .WithName("CrearUsuario")
        .Produces<UsuarioDto>(201)
        .ProducesProblem(400);

        // PUT /api/usuarios/{id}
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarUsuarioDto dto,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null || user.IdEmpresa != currentUser.IdEmpresa)
                return Results.NotFound();

            // Validar que no cambie a un username duplicado
            var existente = await userManager.FindByNameAsync(dto.UserName);
            if (existente != null && existente.Id != user.Id)
                return Results.BadRequest(new { message = $"Ya existe otro usuario con el nombre de usuario '{dto.UserName}'." });

            user.Nombre = dto.Nombre.Trim();
            user.UserName = dto.UserName.Trim();
            user.Legajo = dto.Legajo?.Trim();
            user.Bloqueado = !dto.Activo;
            if (dto.Activo)
            {
                user.IntentosFallidos = 0;
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return Results.BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            // Actualizar permisos
            var existingClaims = await userManager.GetClaimsAsync(user);
            var permissionClaims = existingClaims.Where(c => c.Type == NewgestClaims.Permiso).ToList();
            await userManager.RemoveClaimsAsync(user, permissionClaims);

            if (dto.Permisos != null && dto.Permisos.Any())
            {
                var newClaims = dto.Permisos.Select(p => new Claim(NewgestClaims.Permiso, p)).ToList();
                await userManager.AddClaimsAsync(user, newClaims);
            }

            var responseDto = new UsuarioDto(
                user.Id,
                user.UserName,
                user.Nombre,
                user.Legajo,
                user.UltimoLogin,
                user.Bloqueado && user.IntentosFallidos >= 5,
                !user.Bloqueado,
                dto.Permisos ?? new List<string>()
            );

            return Results.Ok(responseDto);
        })
        .WithName("ActualizarUsuario")
        .Produces<UsuarioDto>()
        .ProducesProblem(400)
        .ProducesProblem(404);

        // POST /api/usuarios/{id}/resetear-password
        group.MapPost("/{id:int}/resetear-password", async (
            int id,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null || user.IdEmpresa != currentUser.IdEmpresa)
                return Results.NotFound();

            await userManager.RemovePasswordAsync(user);
            var result = await userManager.AddPasswordAsync(user, "NewGest1234");
            if (!result.Succeeded)
                return Results.BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            user.DebeResetearPassword = true;
            await userManager.UpdateAsync(user);

            return Results.NoContent();
        })
        .WithName("ResetearPassword")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404);

        // GET /api/usuarios/modulos-permisos
        group.MapGet("/modulos-permisos", () => Results.Ok(new[]
        {
            new { id = "M02", nombre = "Autenticación" },
            new { id = "M04", nombre = "Facturación AFIP" },
            new { id = "M05", nombre = "Facturación Blanca" },
            new { id = "M06", nombre = "Cobranzas" },
            new { id = "M08", nombre = "Clientes" },
            new { id = "M09", nombre = "Stock" },
            new { id = "M10", nombre = "Artículos" },
            new { id = "M11", nombre = "Pedidos" },
            new { id = "M12", nombre = "Contabilidad" },
            new { id = "M13", nombre = "Personal" },
            new { id = "M15", nombre = "Reportes" },
            new { id = "M18", nombre = "Exportaciones" },
            new { id = "ADM", nombre = "Administración" }
        }))
        .WithName("GetModulosPermisos")
        .Produces<object>();

        return app;
    }
}

public record CrearUsuarioDto(
    string Nombre,
    string UserName,
    string? Legajo,
    List<string> Permisos,
    string Password
);

public record ActualizarUsuarioDto(
    string Nombre,
    string UserName,
    string? Legajo,
    List<string> Permisos,
    bool Activo
);

public record UsuarioDto(
    int Id,
    string UserName,
    string Nombre,
    string? Legajo,
    DateTime? UltimoLogin,
    bool Bloqueado,
    bool Activo,
    List<string> Permisos
);
