using MediatR;
using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;
using NewGest.Application.Features.Contabilidad.Queries.GetLibroIva;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;

namespace NewGest.Api.Endpoints;

public static class ReportesEndpoints
{
    public static IEndpointRouteBuilder MapReportesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reportes")
            .WithTags("Reportes")
            .RequireAuthorization();

        // ── PDF comprobante ───────────────────────────────────────────────────
        group.MapGet("/comprobantes/{id:int}/pdf", async (
            int id,
            ICurrentUserService cu, IMediator m,
            IReportService reportService, IQrFiscalService qrService,
            IEmpresaRepository empresasRepo, CancellationToken ct) =>
        {
            var comp = await m.Send(new GetComprobanteByIdQuery(id, cu.IdEmpresa), ct);
            if (comp is null) return Results.NotFound();

            var empresa = await empresasRepo.ObtenerPorIdAsync(cu.IdEmpresa, ct);
            if (empresa is null) return Results.NotFound();

            var tipoLabel = comp.Tipo switch
            {
                "FacturaA"    => "FACTURA A",
                "FacturaB"    => "FACTURA B",
                "FacturaC"    => "FACTURA C",
                "NotaCreditoA" => "NOTA DE CRÉDITO A",
                "NotaCreditoB" => "NOTA DE CRÉDITO B",
                "NotaDebitoA"  => "NOTA DE DÉBITO A",
                "NotaDebitoB"  => "NOTA DE DÉBITO B",
                _             => comp.Tipo
            };

            var datos = new ComprobanteReportDto(
                NombreEmpresa:        empresa.Nombre,
                RazonSocialEmpresa:   empresa.RazonSocial ?? empresa.Nombre,
                CuitEmpresa:          empresa.Cuit ?? "",
                CondicionIvaEmpresa:  "Responsable Inscripto",
                DomicilioEmpresa:     "",
                TipoLabel:            tipoLabel,
                PuntoVenta:           comp.PuntoVenta,
                Numero:               comp.Numero,
                Fecha:                comp.Fecha,
                RazonSocialCliente:   comp.RazonSocialCliente,
                CuitCliente:          comp.CuitCliente,
                CondicionIvaCliente:  comp.CondicionIvaReceptor,
                DomicilioCliente:     null,
                Items: comp.Items.Select(i => new ItemReportDto(
                    i.Descripcion, i.Cantidad, i.PrecioUnitario, i.Alicuota, i.Subtotal)).ToList(),
                Alicuotas: comp.Items
                    .GroupBy(i => i.Alicuota)
                    .Select(g => new AlicuotaReportDto(
                        g.Key,
                        g.Sum(i => i.SubtotalNeto),
                        g.Sum(i => i.Iva)))
                    .ToList(),
                TotalNeto:  comp.TotalNeto,
                TotalIva:   comp.TotalIva,
                Total:      comp.Total,
                CodigoCae:         comp.CodigoCae,
                VencimientoCae:    comp.FechaVencimientoCae,
                QrUrl:             comp.CodigoCae is not null ? $"https://www.afip.gob.ar/fe/qr/?p={comp.CodigoCae}" : null
            );

            var pdf = await reportService.GenerarComprobanteAsync(datos, ct);
            return Results.File(pdf, "application/pdf",
                $"comprobante_{comp.PuntoVenta:D4}-{comp.Numero:D8}.pdf");
        })
        .WithName("GetComprobantePdf")
        .Produces<byte[]>(200, "application/pdf")
        .ProducesProblem(404);

        // ── PDF Libro IVA ─────────────────────────────────────────────────────
        group.MapGet("/libro-iva/pdf", async (
            int anio, int mes, string tipo,
            ICurrentUserService cu, IMediator m,
            IReportService reportService, IEmpresaRepository empresasRepo,
            CancellationToken ct) =>
        {
            if (anio < 2000 || mes is < 1 or > 12) return Results.BadRequest("Año o mes inválidos.");
            var tipoLibro = tipo.ToLower() == "compras" ? TipoLibroIva.Compras : TipoLibroIva.Ventas;

            var libroData = await m.Send(new GetLibroIvaQuery(cu.IdEmpresa, anio, mes, tipoLibro), ct);
            var empresa   = await empresasRepo.ObtenerPorIdAsync(cu.IdEmpresa, ct);

            var reportDto = new LibroIvaReportDto(
                empresa?.Nombre ?? "",
                empresa?.Cuit ?? "",
                anio, mes,
                libroData.TipoLibro,
                libroData.Lineas.Select(l => new LineaLibroIvaReportDto(
                    l.Fecha, l.TipoComprobante, l.RazonSocial, l.Cuit,
                    l.TotalNeto, l.TotalIva, l.Total)).ToList(),
                libroData.TotalNeto, libroData.TotalIva, libroData.TotalGeneral);

            var pdf = await reportService.GenerarLibroIvaPdfAsync(reportDto, ct);
            return Results.File(pdf, "application/pdf",
                $"libro-iva-{tipo}-{anio}-{mes:D2}.pdf");
        })
        .WithName("GetLibroIvaPdf")
        .Produces<byte[]>(200, "application/pdf")
        .ProducesProblem(400);

        // ── Excel Libro IVA ───────────────────────────────────────────────────
        group.MapGet("/libro-iva/excel", async (
            int anio, int mes, string tipo,
            ICurrentUserService cu, IMediator m,
            IExcelExportService excelService, IEmpresaRepository empresasRepo,
            CancellationToken ct) =>
        {
            if (anio < 2000 || mes is < 1 or > 12) return Results.BadRequest("Año o mes inválidos.");
            var tipoLibro = tipo.ToLower() == "compras" ? TipoLibroIva.Compras : TipoLibroIva.Ventas;

            var libroData = await m.Send(new GetLibroIvaQuery(cu.IdEmpresa, anio, mes, tipoLibro), ct);
            var empresa   = await empresasRepo.ObtenerPorIdAsync(cu.IdEmpresa, ct);

            var reportDto = new LibroIvaReportDto(
                empresa?.Nombre ?? "",
                empresa?.Cuit ?? "",
                anio, mes, libroData.TipoLibro,
                libroData.Lineas.Select(l => new LineaLibroIvaReportDto(
                    l.Fecha, l.TipoComprobante, l.RazonSocial, l.Cuit,
                    l.TotalNeto, l.TotalIva, l.Total)).ToList(),
                libroData.TotalNeto, libroData.TotalIva, libroData.TotalGeneral);

            var xlsx = excelService.ExportarLibroIva(reportDto);
            return Results.File(xlsx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"libro-iva-{tipo}-{anio}-{mes:D2}.xlsx");
        })
        .WithName("GetLibroIvaExcel")
        .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        .ProducesProblem(400);

        // ── Excel Clientes ────────────────────────────────────────────────────
        group.MapGet("/clientes/excel", async (
            ICurrentUserService cu, IMediator m,
            IExcelExportService excelService, CancellationToken ct) =>
        {
            var clientes = await m.Send(
                new Application.Features.Clientes.Queries.GetClientes.GetClientesQuery(
                    cu.IdEmpresa, null, null, null, 1, 99999), ct);

            var datos = clientes.Items.Select(c => new ClienteExportDto(
                c.Codigo, c.RazonSocial, c.CUIT, c.CondicionIvaDescripcion,
                c.Localidad, null, null));

            var xlsx = excelService.ExportarClientes(datos);
            return Results.File(xlsx,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "clientes.xlsx");
        })
        .WithName("ExportarClientesExcel")
        .Produces<byte[]>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        return app;
    }
}
