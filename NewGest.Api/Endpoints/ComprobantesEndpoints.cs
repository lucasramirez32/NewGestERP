using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;
using NewGest.Application.Features.Comprobantes.Commands.EmitirLote;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobantes;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class ComprobantesEndpoints
{
    public static IEndpointRouteBuilder MapComprobantesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comprobantes")
            .WithTags("Facturación")
            .RequireAuthorization();

        // GET /api/comprobantes?pagina=1&tamano=20
        group.MapGet("/", async (
            int pagina, int tamano,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetComprobantesQuery(
                cu.IdEmpresa,
                pagina <= 0 ? 1 : pagina,
                tamano <= 0 ? 20 : tamano), ct);
            return Results.Ok(result);
        })
        .WithName("GetComprobantes")
        .Produces<PagedResult<ComprobanteDto>>();

        // GET /api/comprobantes/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetComprobanteByIdQuery(id, cu.IdEmpresa), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetComprobanteById")
        .Produces<ComprobanteDto>()
        .ProducesProblem(404);

        // POST /api/comprobantes
        group.MapPost("/", async (
            EmitirFacturaDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new EmitirFacturaCommand(
                cu.IdEmpresa,
                dto.Tipo,
                dto.PuntoVenta,
                dto.Fecha,
                dto.IdCliente,
                dto.IdPedidoOrigen,
                dto.Items), ct);
            return Results.Created($"/api/comprobantes/{result.IdComprobante}", result);
        })
        .WithName("EmitirFactura")
        .Produces<FacturaEmitidaDto>(201)
        .ProducesProblem(400);

        // POST /api/comprobantes/lote
        group.MapPost("/lote", async (
            EmitirLoteDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var commands = dto.Comprobantes.Select(c => new EmitirFacturaCommand(
                cu.IdEmpresa,
                c.Tipo,
                c.PuntoVenta,
                c.Fecha,
                c.IdCliente,
                c.IdPedidoOrigen,
                c.Items)).ToList();

            var result = await m.Send(new EmitirLoteCommand(cu.IdEmpresa, commands), ct);
            return Results.Ok(result);
        })
        .WithName("EmitirLote")
        .Produces<LoteResultDto>()
        .ProducesProblem(400);

        return app;
    }
}
