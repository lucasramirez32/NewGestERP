using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Features.Stock.Commands.CrearDeposito;
using NewGest.Application.Features.Stock.Commands.RegistrarMovimiento;
using NewGest.Application.Features.Stock.Queries.GetAlertasReposicion;
using NewGest.Application.Features.Stock.Queries.GetDepositos;
using NewGest.Application.Features.Stock.Queries.GetExistencias;
using NewGest.Application.Features.Stock.Queries.GetHistorialMovimientos;
using NewGest.Application.Features.Stock.Queries.GetStockValorizado;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        // ─── Depósitos ────────────────────────────────────────────────────────
        var depositos = app.MapGroup("/api/depositos")
            .WithTags("Stock - Depósitos")
            .RequireAuthorization();

        depositos.MapGet("/", async (
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetDepositosQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        }).WithName("GetDepositos").Produces<IReadOnlyList<DepositoDto>>();

        depositos.MapPost("/", async (
            CrearDepositoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearDepositoCommand(cu.IdEmpresa, dto.Codigo, dto.Descripcion), ct);
            return Results.Created($"/api/depositos/{result.IdDeposito}", result);
        }).WithName("CrearDeposito").Produces<DepositoDto>(201).ProducesProblem(400);

        // ─── Stock ────────────────────────────────────────────────────────────
        var stock = app.MapGroup("/api/stock")
            .WithTags("Stock - Movimientos")
            .RequireAuthorization();

        // GET /api/stock/{idArticulo}/existencias
        stock.MapGet("/{idArticulo:int}/existencias", async (
            int idArticulo,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetExistenciasQuery(cu.IdEmpresa, idArticulo), ct);
            return Results.Ok(result);
        }).WithName("GetExistencias").Produces<IReadOnlyList<ExistenciaDepositoDto>>();

        // GET /api/stock/{idArticulo}/movimientos
        stock.MapGet("/{idArticulo:int}/movimientos", async (
            int idArticulo,
            int page,
            int pageSize,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetHistorialMovimientosQuery(
                cu.IdEmpresa, idArticulo,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize), ct);
            return Results.Ok(result);
        }).WithName("GetHistorialMovimientos");

        // POST /api/stock/movimientos
        stock.MapPost("/movimientos", async (
            RegistrarMovimientoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var cmd = new RegistrarMovimientoCommand(
                cu.IdEmpresa, dto.IdArticulo, dto.IdDeposito, dto.Tipo,
                dto.Cantidad, dto.CostoUnitario, dto.NumeroSerie,
                dto.IdComprobanteOrigen, dto.Observaciones);

            var result = await m.Send(cmd, ct);
            return Results.Created($"/api/stock/{dto.IdArticulo}/movimientos", result);
        }).WithName("RegistrarMovimiento").Produces<MovimientoStockDto>(201).ProducesProblem(400);

        // GET /api/stock/alertas-reposicion
        stock.MapGet("/alertas-reposicion", async (
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetAlertasReposicionQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        }).WithName("GetAlertasReposicion").Produces<IReadOnlyList<ExistenciaDepositoDto>>();

        // GET /api/stock/valorizado
        stock.MapGet("/valorizado", async (
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetStockValorizadoQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        }).WithName("GetStockValorizado");

        return app;
    }
}
