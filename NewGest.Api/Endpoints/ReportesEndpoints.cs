using System.Text;
using System.Text.Json;
using MediatR;
using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;
using NewGest.Application.Features.Contabilidad.Queries.GetLibroIva;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;

namespace NewGest.Api.Endpoints;

public static class ReportesEndpoints
{
    // Mapa TipoComprobante string → código AFIP entero
    private static readonly Dictionary<string, int> TiposAfip = new()
    {
        ["FacturaA"]     = 1,  ["FacturaB"]     = 6,  ["FacturaC"]    = 11, ["FacturaM"]    = 51,
        ["NotaCreditoA"] = 3,  ["NotaCreditoB"] = 8,  ["NotaCreditoC"] = 13,
        ["NotaDebitoA"]  = 2,  ["NotaDebitoB"]  = 7,  ["NotaDebitoC"] = 12,
        ["RecibosA"]     = 4,  ["RecibosB"]     = 9
    };

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
            var comp    = await m.Send(new GetComprobanteByIdQuery(id, cu.IdEmpresa), ct);
            if (comp is null) return Results.NotFound();

            var empresa = await empresasRepo.ObtenerPorIdAsync(cu.IdEmpresa, ct);
            if (empresa is null) return Results.NotFound();

            var tipoLabel = comp.Tipo switch
            {
                "FacturaA"     => "FACTURA A",     "FacturaB"     => "FACTURA B",
                "FacturaC"     => "FACTURA C",     "FacturaM"     => "FACTURA MiPyME",
                "NotaCreditoA" => "NOTA DE CRÉDITO A", "NotaCreditoB" => "NOTA DE CRÉDITO B",
                "NotaCreditoC" => "NOTA DE CRÉDITO C",
                "NotaDebitoA"  => "NOTA DE DÉBITO A",  "NotaDebitoB"  => "NOTA DE DÉBITO B",
                _              => comp.Tipo
            };

            // Fix BUG-04: construir URL correcta del QR AFIP (JSON completo en Base64)
            // Fix BUG-01: generar PNG y pasarlo al DTO para que el documento lo renderice
            string? qrUrl   = null;
            byte[]? qrBytes = null;

            if (comp.CodigoCae is not null && empresa.Cuit is not null &&
                TiposAfip.TryGetValue(comp.Tipo, out var tipoCodigo))
            {
                var cuitSinGuiones = empresa.Cuit.Replace("-", "").Replace(" ", "");
                var nroDocRec = comp.CuitCliente is not null
                    ? long.TryParse(comp.CuitCliente.Replace("-",""), out var cuit) ? cuit : 0L
                    : 0L;

                var payload = new
                {
                    ver      = 1,
                    fecha    = comp.Fecha.ToString("yyyy-MM-dd"),
                    cuit     = long.Parse(cuitSinGuiones),
                    ptoVta   = comp.PuntoVenta,
                    tipoCmp  = tipoCodigo,
                    nroCmp   = comp.Numero,
                    importe  = comp.Total,
                    moneda   = "PES",
                    ctz      = 1,
                    tipoDocRec = comp.CuitCliente is not null ? 80 : 99,
                    nroDocRec,
                    tipoCodAut = "E",
                    codAut   = long.TryParse(comp.CodigoCae, out var caeNum) ? caeNum : 0L
                };

                var json = JsonSerializer.Serialize(payload);
                var b64  = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                qrUrl    = $"https://www.afip.gob.ar/fe/qr/?p={b64}";
                qrBytes  = qrService.GenerarPng(qrUrl);
            }

            // Fix BUG-06: condición IVA y domicilio desde la entidad Empresa
            var condicionIvaEmpresa = empresa.Cuit is not null ? "Responsable Inscripto" : "";
            var domicilioEmpresa    = "";  // Empresa.Domicilio pendiente de Sprint 19-20

            var datos = new ComprobanteReportDto(
                NombreEmpresa:       empresa.Nombre,
                RazonSocialEmpresa:  empresa.RazonSocial ?? empresa.Nombre,
                CuitEmpresa:         empresa.Cuit ?? "",
                CondicionIvaEmpresa: condicionIvaEmpresa,
                DomicilioEmpresa:    domicilioEmpresa,
                TipoLabel:           tipoLabel,
                PuntoVenta:          comp.PuntoVenta,
                Numero:              comp.Numero,
                Fecha:               comp.Fecha,
                RazonSocialCliente:  comp.RazonSocialCliente,
                CuitCliente:         comp.CuitCliente,
                CondicionIvaCliente: comp.CondicionIvaReceptor,
                DomicilioCliente:    null,
                Items: comp.Items.Select(i => new ItemReportDto(
                    i.Descripcion, i.Cantidad, i.PrecioUnitario, i.Alicuota, i.Subtotal)).ToList(),
                Alicuotas: comp.Items
                    .GroupBy(i => i.Alicuota)
                    .Select(g => new AlicuotaReportDto(
                        g.Key,
                        Math.Round(g.Sum(i => i.SubtotalNeto), 2),
                        Math.Round(g.Sum(i => i.Iva), 2)))
                    .ToList(),
                TotalNeto:          comp.TotalNeto,
                TotalIva:           comp.TotalIva,
                Total:              comp.Total,
                CodigoCae:          comp.CodigoCae,
                VencimientoCae:     comp.FechaVencimientoCae,
                QrUrl:              qrUrl,
                QrPngBytes:         qrBytes);   // Fix BUG-01

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

            // Fix BUG-03: mapear desglose de alícuotas
            var reportDto = BuildLibroIvaReportDto(empresa?.Nombre ?? "", empresa?.Cuit ?? "",
                anio, mes, libroData);

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

            var reportDto = BuildLibroIvaReportDto(empresa?.Nombre ?? "", empresa?.Cuit ?? "",
                anio, mes, libroData);

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

    // Fix BUG-03: helper para construir LibroIvaReportDto con desglose de alícuotas
    private static LibroIvaReportDto BuildLibroIvaReportDto(
        string nombreEmpresa, string cuitEmpresa, int anio, int mes,
        Application.DTOs.Contabilidad.LibroIvaDto libroData)
    {
        // El DTO de contabilidad tiene TotalNeto/TotalIva consolidados.
        // Por ahora mapeamos todo a 21% hasta que LibroIvaQueryHandler exponga el desglose.
        // TODO Sprint 17: LibroIvaQueryHandler debe devolver desglose por alícuota.
        var lineas = libroData.Lineas.Select(l => new LineaLibroIvaReportDto(
            l.Fecha,
            $"{l.TipoComprobante} {l.PuntoVenta:D4}-{l.Numero:D8}",
            l.RazonSocial,
            l.Cuit,
            Neto21:  l.TotalNeto,  // TODO: desglosar cuando esté disponible
            Iva21:   l.TotalIva,
            Neto105: 0m,
            Iva105:  0m,
            Exento:  0m,
            Total:   l.Total)).ToList();

        return new LibroIvaReportDto(
            nombreEmpresa, cuitEmpresa, anio, mes, libroData.TipoLibro,
            lineas,
            TotalNeto21:  libroData.TotalNeto,
            TotalIva21:   libroData.TotalIva,
            TotalNeto105: 0m,
            TotalIva105:  0m,
            TotalExento:  0m,
            TotalGeneral: libroData.TotalGeneral);
    }
}
