using MediatR;
using NewGest.Application.DTOs.Cobranzas;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Cobranzas.Commands.CrearPago;
using NewGest.Application.Features.Cobranzas.Queries.GetPendientes;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class CobranzasEndpoints
{
    public static IEndpointRouteBuilder MapCobranzasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cobranzas")
            .WithTags("Cobranzas")
            .RequireAuthorization();

        // GET /api/cobranzas/pendientes/{idCliente} — comprobantes sin cobrar
        group.MapGet("/pendientes/{idCliente:int}", async (
            int idCliente,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetPendientesQuery(cu.IdEmpresa, idCliente), ct)))
            .WithName("GetPendientes")
            .Produces<IReadOnlyList<ComprobantePendienteDto>>();

        // POST /api/cobranzas/pagos — crear recibo de cobro
        group.MapPost("/pagos", async (
            CrearPagoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearPagoCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/cobranzas/pagos/{result.IdPago}", result);
        })
        .WithName("CrearPago")
        .Produces<PagoDto>(201)
        .ProducesProblem(400);

        return app;
    }
}
