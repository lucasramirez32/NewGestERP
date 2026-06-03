using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Articulos.Commands.ActualizarArticulo;
using NewGest.Application.Features.Articulos.Commands.CrearArticulo;
using NewGest.Application.Features.Articulos.Commands.CrearGrupoArticulo;
using NewGest.Application.Features.Articulos.Commands.DesactivarArticulo;
using NewGest.Application.Features.Articulos.Queries.GetArticuloById;
using NewGest.Application.Features.Articulos.Queries.GetArticulos;
using NewGest.Application.Features.Articulos.Queries.GetGruposArticulos;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class ArticulosEndpoints
{
    public static IEndpointRouteBuilder MapArticulosEndpoints(this IEndpointRouteBuilder app)
    {
        var artGroup = app.MapGroup("/api/articulos")
            .WithTags("Articulos")
            .RequireAuthorization();

        var gruposGroup = app.MapGroup("/api/grupos-articulos")
            .WithTags("Articulos")
            .RequireAuthorization();

        // GET /api/articulos
        artGroup.MapGet("/", async (
            string? search,
            int? idGrupo,
            int page,
            int pageSize,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetArticulosQuery(
                currentUser.IdEmpresa,
                search,
                idGrupo,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize);

            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetArticulos")
        .Produces<PagedResult<ArticuloListItemDto>>();

        // GET /api/articulos/{id}
        artGroup.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetArticuloByIdQuery(currentUser.IdEmpresa, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetArticuloById")
        .Produces<ArticuloDto>()
        .ProducesProblem(404);

        // POST /api/articulos
        artGroup.MapPost("/", async (
            CrearArticuloDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CrearArticuloCommand(
                currentUser.IdEmpresa,
                dto.Codigo,
                dto.Descripcion,
                dto.IdGrupo,
                dto.IdUnidad,
                dto.PrecioLista,
                dto.PrecioCosto,
                dto.PorcentajeIva,
                dto.Observaciones);

            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/articulos/{result.IdArticulo}", result);
        })
        .WithName("CrearArticulo")
        .Produces<ArticuloDto>(201)
        .ProducesProblem(400)
        .ProducesProblem(422);

        // PUT /api/articulos/{id}
        artGroup.MapPut("/{id:int}", async (
            int id,
            ActualizarArticuloDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ActualizarArticuloCommand(
                currentUser.IdEmpresa,
                id,
                dto.Descripcion,
                dto.IdGrupo,
                dto.IdUnidad,
                dto.PrecioLista,
                dto.PrecioCosto,
                dto.PorcentajeIva,
                dto.Observaciones);

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ActualizarArticulo")
        .Produces<ArticuloDto>()
        .ProducesProblem(400)
        .ProducesProblem(404);

        // DELETE /api/articulos/{id}  — soft delete
        artGroup.MapDelete("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DesactivarArticuloCommand(currentUser.IdEmpresa, id), ct);
            return Results.NoContent();
        })
        .WithName("DesactivarArticulo")
        .Produces(204)
        .ProducesProblem(404);

        // GET /api/articulos/{id}/existencias — stub Sprint 5
        artGroup.MapGet("/{id:int}/existencias", (int id, ICurrentUserService _) =>
            Results.Ok(new { existencias = 0m }))
        .WithName("GetExistenciasArticulo")
        .Produces<object>();

        // GET /api/grupos-articulos
        gruposGroup.MapGet("/", async (
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetGruposArticulosQuery(currentUser.IdEmpresa), ct);
            return Results.Ok(result);
        })
        .WithName("GetGruposArticulos")
        .Produces<IReadOnlyList<GrupoArticuloDto>>();

        // POST /api/grupos-articulos
        gruposGroup.MapPost("/", async (
            CrearGrupoArticuloCommand command,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // Reemplazar IdEmpresa del body por el del JWT
            var cmd = command with { IdEmpresa = currentUser.IdEmpresa };
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/grupos-articulos/{result.IdGrupo}", result);
        })
        .WithName("CrearGrupoArticulo")
        .Produces<GrupoArticuloDto>(201)
        .ProducesProblem(400);

        return app;
    }
}
