using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Pedidos.Commands.AnularPedido;
using NewGest.Application.Features.Pedidos.Commands.CrearPedido;
using NewGest.Application.Features.Pedidos.Commands.GenerarRemito;
using NewGest.Application.Features.Pedidos.Queries.GetPedidoById;
using NewGest.Application.Features.Pedidos.Queries.GetPedidos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Enums;

namespace NewGest.Api.Endpoints;

public static class PedidosEndpoints
{
    public static IEndpointRouteBuilder MapPedidosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos")
            .WithTags("Pedidos y Remitos")
            .RequireAuthorization();

        // GET /api/pedidos
        group.MapGet("/", async (
            int? estado,
            int page,
            int pageSize,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var query = new GetPedidosQuery(
                cu.IdEmpresa,
                estado.HasValue ? (EstadoPedido)estado.Value : null,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize);

            var result = await m.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetPedidos")
        .Produces<PagedResult<PedidoListItemDto>>();

        // GET /api/pedidos/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetPedidoByIdQuery(cu.IdEmpresa, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetPedidoById")
        .Produces<PedidoDto>()
        .ProducesProblem(404);

        // POST /api/pedidos
        group.MapPost("/", async (
            CrearPedidoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearPedidoCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/pedidos/{result.IdPedido}", result);
        })
        .WithName("CrearPedido")
        .Produces<PedidoDto>(201)
        .ProducesProblem(400);

        // POST /api/pedidos/{id}/anular
        group.MapPost("/{id:int}/anular", async (
            int id,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new AnularPedidoCommand(cu.IdEmpresa, id), ct);
            return Results.NoContent();
        })
        .WithName("AnularPedido")
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(404);

        // POST /api/pedidos/{id}/remito
        group.MapPost("/{id:int}/remito", async (
            int id,
            GenerarRemitoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GenerarRemitoCommand(cu.IdEmpresa, id, dto), ct);
            return Results.Created($"/api/pedidos/{id}/remito/{result.IdRemito}", result);
        })
        .WithName("GenerarRemito")
        .Produces<RemitoDto>(201)
        .ProducesProblem(400)
        .ProducesProblem(404);

        return app;
    }
}
