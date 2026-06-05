using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Contabilidad.Commands.CrearAsiento;
using NewGest.Application.Features.Contabilidad.Commands.CrearCuentaContable;
using NewGest.Application.Features.Contabilidad.Queries.GetAsientos;
using NewGest.Application.Features.Contabilidad.Queries.GetLibroIva;
using NewGest.Application.Features.Contabilidad.Queries.GetPlanCuentas;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class ContabilidadEndpoints
{
    public static IEndpointRouteBuilder MapContabilidadEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Plan de Cuentas ──────────────────────────────────────────────────
        var cuentas = app.MapGroup("/api/cuentas-contables")
            .WithTags("Contabilidad — Plan de Cuentas")
            .RequireAuthorization();

        cuentas.MapGet("/", async (ICurrentUserService cu, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetPlanCuentasQuery(cu.IdEmpresa), ct)))
            .WithName("GetPlanCuentas")
            .Produces<IReadOnlyList<CuentaContableDto>>();

        cuentas.MapPost("/", async (
            CrearCuentaContableDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearCuentaContableCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/cuentas-contables/{result.IdCuenta}", result);
        })
        .WithName("CrearCuentaContable")
        .Produces<CuentaContableDto>(201)
        .ProducesProblem(400);

        // ── Asientos ─────────────────────────────────────────────────────────
        var asientos = app.MapGroup("/api/asientos")
            .WithTags("Contabilidad — Asientos")
            .RequireAuthorization();

        asientos.MapGet("/", async (
            int pagina, int tamano,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetAsientosQuery(
                cu.IdEmpresa,
                pagina <= 0 ? 1 : pagina,
                tamano <= 0 ? 20 : tamano), ct)))
            .WithName("GetAsientos")
            .Produces<PagedResult<AsientoDto>>();

        asientos.MapPost("/", async (
            CrearAsientoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearAsientoCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/asientos/{result.IdAsiento}", result);
        })
        .WithName("CrearAsiento")
        .Produces<AsientoDto>(201)
        .ProducesProblem(400);

        // ── Libro IVA ─────────────────────────────────────────────────────────
        var libroIva = app.MapGroup("/api/libro-iva")
            .WithTags("Contabilidad — Libro IVA")
            .RequireAuthorization();

        libroIva.MapGet("/ventas", async (
            int anio, int mes,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            if (anio < 2000 || mes is < 1 or > 12)
                return Results.BadRequest("Año o mes inválidos.");
            return Results.Ok(await m.Send(
                new GetLibroIvaQuery(cu.IdEmpresa, anio, mes, TipoLibroIva.Ventas), ct));
        })
        .WithName("GetLibroIvaVentas")
        .Produces<LibroIvaDto>()
        .ProducesProblem(400);

        libroIva.MapGet("/compras", async (
            int anio, int mes,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            if (anio < 2000 || mes is < 1 or > 12)
                return Results.BadRequest("Año o mes inválidos.");
            return Results.Ok(await m.Send(
                new GetLibroIvaQuery(cu.IdEmpresa, anio, mes, TipoLibroIva.Compras), ct));
        })
        .WithName("GetLibroIvaCompras")
        .Produces<LibroIvaDto>()
        .ProducesProblem(400);

        return app;
    }
}
