using MediatR;
using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Clientes.Commands.ActualizarCliente;
using NewGest.Application.Features.Clientes.Commands.CrearCliente;
using NewGest.Application.Features.Clientes.Commands.DesactivarCliente;
using NewGest.Application.Features.Clientes.Queries.GetClienteById;
using NewGest.Application.Features.Clientes.Queries.GetClientes;
using NewGest.Application.Interfaces;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Api.Endpoints;

public record AnaliticoItemDto(DateTime Fecha, string Tipo, string Numero, decimal Debe, decimal Haber, decimal Saldo);
public record HistoricoArticuloDto(DateOnly Fecha, string CodigoArticulo, string DescripcionArticulo, decimal Cantidad, decimal PrecioUnitario, decimal Subtotal, string ComprobanteInfo);

public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .RequireAuthorization();

        // GET /api/clientes
        group.MapGet("/", async (
            string? search,
            int? condicionIva,
            int? zona,
            int page,
            int pageSize,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetClientesQuery(
                currentUser.IdEmpresa,
                search,
                condicionIva.HasValue ? (CondicionIva)condicionIva.Value : null,
                zona,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize);

            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetClientes")
        .Produces<PagedResult<ClienteListItemDto>>();

        // GET /api/clientes/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetClienteByIdQuery(currentUser.IdEmpresa, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetClienteById")
        .Produces<ClienteDto>()
        .ProducesProblem(404);

        // POST /api/clientes
        group.MapPost("/", async (
            CrearClienteDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CrearClienteCommand(
                currentUser.IdEmpresa,
                dto.Codigo,
                dto.RazonSocial,
                dto.CUIT,
                dto.CondicionIva,
                dto.Domicilio,
                dto.Localidad,
                dto.Telefono,
                dto.Email,
                dto.IdZona,
                dto.Observaciones,
                dto.NombreFantasia,
                dto.LimiteCredito,
                dto.DiasMora,
                dto.Descuento,
                dto.Provincia,
                dto.CodigoPostal,
                dto.ObraSocial,
                dto.NroAfiliado,
                dto.MedicoCabecera,
                dto.MatriculaMedico,
                dto.Alergia,
                dto.Alergias,
                dto.Tratamiento,
                dto.Convulsiones,
                dto.Medicacion,
                dto.Patologia);

            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/clientes/{result.IdCliente}", result);
        })
        .WithName("CrearCliente")
        .Produces<ClienteDto>(201)
        .ProducesProblem(400)
        .ProducesProblem(422);

        // PUT /api/clientes/{id}
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarClienteDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ActualizarClienteCommand(
                currentUser.IdEmpresa,
                id,
                dto.RazonSocial,
                dto.CUIT,
                dto.CondicionIva,
                dto.Domicilio,
                dto.Localidad,
                dto.Telefono,
                dto.Email,
                dto.IdZona,
                dto.Observaciones,
                dto.NombreFantasia,
                dto.LimiteCredito,
                dto.DiasMora,
                dto.Descuento,
                dto.Provincia,
                dto.CodigoPostal,
                dto.ObraSocial,
                dto.NroAfiliado,
                dto.MedicoCabecera,
                dto.MatriculaMedico,
                dto.Alergia,
                dto.Alergias,
                dto.Tratamiento,
                dto.Convulsiones,
                dto.Medicacion,
                dto.Patologia);

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ActualizarCliente")
        .Produces<ClienteDto>()
        .ProducesProblem(400)
        .ProducesProblem(404);

        // DELETE /api/clientes/{id}  — soft delete
        group.MapDelete("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DesactivarClienteCommand(currentUser.IdEmpresa, id), ct);
            return Results.NoContent();
        })
        .WithName("DesactivarCliente")
        .Produces(204)
        .ProducesProblem(404);

        // GET /api/clientes/{id}/saldo-pendiente
        group.MapGet("/{id:int}/saldo-pendiente", async (
            int id,
            ICurrentUserService currentUser,
            NewgestDbContext db,
            CancellationToken ct) =>
        {
            var idEmpresa = currentUser.IdEmpresa;

            var totalFacturas = await db.Comprobantes
                .Where(c => c.IdCliente == id && !c.Anulado && c.IdEmpresa == idEmpresa)
                .SumAsync(c => c.Total, ct);

            var totalPagos = await db.Pagos
                .Where(p => p.IdCliente == id && !p.Anulado && p.IdEmpresa == idEmpresa)
                .SumAsync(p => p.TotalMedios, ct);

            return Results.Ok(new { saldoPendiente = Math.Max(0m, totalFacturas - totalPagos) });
        })
        .WithName("GetSaldoPendienteCliente")
        .Produces<object>();

        // GET /api/clientes/{id}/analitico
        group.MapGet("/{id:int}/analitico", async (
            int id,
            ICurrentUserService currentUser,
            NewgestDbContext db,
            CancellationToken ct) =>
        {
            var idEmpresa = currentUser.IdEmpresa;

            var comprobantes = await db.Comprobantes
                .Where(c => c.IdCliente == id && !c.Anulado && c.IdEmpresa == idEmpresa)
                .ToListAsync(ct);

            var pagos = await db.Pagos
                .Where(p => p.IdCliente == id && !p.Anulado && p.IdEmpresa == idEmpresa)
                .ToListAsync(ct);

            var items = new List<AnaliticoItemDto>();

            foreach (var c in comprobantes)
            {
                var esCredito = c.Tipo is TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC;
                var debe = esCredito ? 0m : c.Total;
                var haber = esCredito ? c.Total : 0m;

                var tipoAbbr = c.Tipo switch
                {
                    TipoComprobante.FacturaA => "FAA",
                    TipoComprobante.FacturaB => "FAB",
                    TipoComprobante.FacturaC => "FAC",
                    TipoComprobante.NotaCreditoA => "NCA",
                    TipoComprobante.NotaCreditoB => "NCB",
                    TipoComprobante.NotaCreditoC => "NCC",
                    TipoComprobante.NotaDebitoA => "NDA",
                    TipoComprobante.NotaDebitoB => "NDB",
                    TipoComprobante.NotaDebitoC => "NDC",
                    TipoComprobante.FacturaM => "FAM",
                    _ => c.Tipo.ToString()
                };

                items.Add(new AnaliticoItemDto(
                    c.Fecha.ToDateTime(TimeOnly.MinValue),
                    tipoAbbr,
                    $"{c.PuntoVenta:D4}-{c.Numero:D8}",
                    debe,
                    haber,
                    0m
                ));
            }

            foreach (var p in pagos)
            {
                items.Add(new AnaliticoItemDto(
                    p.Fecha.ToDateTime(TimeOnly.MinValue),
                    "RC",
                    p.Numero.ToString("D8"),
                    0m,
                    p.TotalMedios,
                    0m
                ));
            }

            var sortedItems = items.OrderBy(i => i.Fecha).ToList();
            decimal runningSaldo = 0m;
            var finalItems = new List<AnaliticoItemDto>();
            foreach (var item in sortedItems)
            {
                runningSaldo += (item.Debe - item.Haber);
                finalItems.Add(item with { Saldo = runningSaldo });
            }

            var saldoCtaCte = runningSaldo;
            var saldoAFavor = pagos.Sum(p => p.SaldoAFavor);

            return Results.Ok(new
            {
                movimientos = finalItems,
                saldoCtaCte,
                saldoAFavor
            });
        })
        .WithName("GetAnaliticoCliente")
        .Produces<object>();

        // GET /api/clientes/{id}/historico-articulos
        group.MapGet("/{id:int}/historico-articulos", async (
            int id,
            ICurrentUserService currentUser,
            NewgestDbContext db,
            CancellationToken ct) =>
        {
            var idEmpresa = currentUser.IdEmpresa;

            var items = await db.Comprobantes
                .Where(c => c.IdCliente == id && !c.Anulado && c.IdEmpresa == idEmpresa)
                .SelectMany(c => c.Items.Select(i => new
                {
                    c.Fecha,
                    c.Tipo,
                    c.PuntoVenta,
                    c.Numero,
                    i.IdArticulo,
                    i.Descripcion,
                    i.Cantidad,
                    i.PrecioUnitario,
                    i.Subtotal
                }))
                .ToListAsync(ct);

            var articleIds = items.Select(i => i.IdArticulo).Distinct().ToList();
            var articlesMap = await db.Articulos
                .Where(a => articleIds.Contains(a.IdArticulo))
                .ToDictionaryAsync(a => a.IdArticulo, a => a.Codigo.Trim(), ct);

            var historico = items.Select(i =>
            {
                var tipoAbbr = i.Tipo switch
                {
                    TipoComprobante.FacturaA => "FAA",
                    TipoComprobante.FacturaB => "FAB",
                    TipoComprobante.FacturaC => "FAC",
                    TipoComprobante.NotaCreditoA => "NCA",
                    TipoComprobante.NotaCreditoB => "NCB",
                    TipoComprobante.NotaCreditoC => "NCC",
                    TipoComprobante.NotaDebitoA => "NDA",
                    TipoComprobante.NotaDebitoB => "NDB",
                    TipoComprobante.NotaDebitoC => "NDC",
                    TipoComprobante.FacturaM => "FAM",
                    _ => i.Tipo.ToString()
                };

                articlesMap.TryGetValue(i.IdArticulo, out var codigoArticulo);

                return new HistoricoArticuloDto(
                    i.Fecha,
                    codigoArticulo ?? "N/A",
                    i.Descripcion,
                    i.Cantidad,
                    i.PrecioUnitario,
                    i.Subtotal,
                    $"{tipoAbbr} {i.PuntoVenta:D4}-{i.Numero:D8}"
                );
            })
            .OrderByDescending(h => h.Fecha)
            .ToList();

            return Results.Ok(historico);
        })
        .WithName("GetHistoricoArticulosCliente")
        .Produces<List<HistoricoArticuloDto>>();

        return app;
    }
}
