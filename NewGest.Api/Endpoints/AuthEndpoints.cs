using MediatR;
using Microsoft.AspNetCore.Authorization;
using NewGest.Application.DTOs;
using NewGest.Application.Features.Auth.Commands.CambiarPassword;
using NewGest.Application.Features.Auth.Commands.Login;
using NewGest.Application.Features.Auth.Queries.GetEmpresas;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // GET /api/auth/empresas — público, para el dropdown de login
        group.MapGet("/empresas", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetEmpresasQuery());
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("GetEmpresas")
        .Produces<IReadOnlyList<EmpresaDto>>();

        // POST /api/auth/login — público
        group.MapPost("/login", async (LoginDto dto, IMediator mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(dto.IdEmpresa, dto.NombreUsuario, dto.Password));
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Login")
        .Produces<LoginResultDto>()
        .ProducesProblem(401);

        // POST /api/auth/cambiar-password — requiere autenticación
        group.MapPost("/cambiar-password", async (
            CambiarPasswordDto dto,
            ICurrentUserService currentUser,
            IMediator mediator) =>
        {
            await mediator.Send(new CambiarPasswordCommand(
                currentUser.UserId,
                dto.PasswordActual,
                dto.PasswordNueva));
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("CambiarPassword")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401);

        return app;
    }
}
