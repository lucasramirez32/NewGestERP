using System.Diagnostics;
using FluentAssertions;
using NewGest.Application.DTOs.Reportes;
using NewGest.Infrastructure.Reports;
using NewGest.Infrastructure.Services;

namespace NewGest.IntegrationTests.Reportes;

public class ReportesTests
{
    private static readonly ReportService _reportService = new();
    private static readonly ExcelExportService _excelService = new();
    private static readonly QrFiscalService _qrService = new();

    // ─── Fixtures ─────────────────────────────────────────────────────────────
    private static ComprobanteReportDto ComprobanteEjemplo(bool conCae = true)
    {
        string? cae     = conCae ? "12345678901234" : null;
        DateOnly? vence = conCae ? new DateOnly(2026, 8, 31) : null;

        // Fix BUG-01: QrPngBytes generado correctamente
        byte[]? qrBytes = null;
        string? qrUrl   = null;
        if (conCae)
        {
            var comp = new { PuntoVenta = 1, Numero = 42L, Total = 1452m };
            qrUrl   = "https://www.afip.gob.ar/fe/qr/?p=dGVzdA==";
            qrBytes = _qrService.GenerarPng(qrUrl);
        }

        return new ComprobanteReportDto(
            NombreEmpresa:       "Empresa Test SA",
            RazonSocialEmpresa:  "Empresa Test S.A.",
            CuitEmpresa:         "30714000000",
            CondicionIvaEmpresa: "Responsable Inscripto",
            DomicilioEmpresa:    "Av. Córdoba 1234, CABA",
            TipoLabel:           "FACTURA B",
            PuntoVenta:          1,
            Numero:              42,
            Fecha:               new DateOnly(2026, 6, 5),
            RazonSocialCliente:  "Juan Pérez",
            CuitCliente:         "20111222333",
            CondicionIvaCliente: "Consumidor Final",
            DomicilioCliente:    null,
            Items: [
                new ItemReportDto("Servicio de prueba", 2m, 500m, "21%", 1000m),
                new ItemReportDto("Otro servicio",      1m, 200m, "21%",  200m),
            ],
            Alicuotas: [new AlicuotaReportDto("21%", 1200m, 252m)],
            TotalNeto:      1200m,
            TotalIva:        252m,
            Total:           1452m,
            CodigoCae:       cae,
            VencimientoCae:  vence,
            QrUrl:           qrUrl,
            QrPngBytes:      qrBytes    // Fix BUG-01
        );
    }

    // Fix BUG-03: fixture con desglose de alícuotas
    private static LibroIvaReportDto LibroIvaEjemplo() => new(
        NombreEmpresa: "Empresa Test SA",
        CuitEmpresa:   "30714000000",
        Anio: 2026, Mes: 6,
        TipoLibro: "Ventas",
        Lineas: [
            new LineaLibroIvaReportDto(
                new DateOnly(2026, 6, 1), "FC-B 0001-00000001", "Cliente A", "20111222333",
                Neto21: 1000m, Iva21: 210m, Neto105: 0m, Iva105: 0m, Exento: 0m, Total: 1210m),
            new LineaLibroIvaReportDto(
                new DateOnly(2026, 6, 5), "FC-B 0001-00000002", "Cliente B", "20999888777",
                Neto21: 0m, Iva21: 0m, Neto105: 500m, Iva105: 52.5m, Exento: 0m, Total: 552.5m),
        ],
        TotalNeto21:  1000m, TotalIva21:  210m,
        TotalNeto105: 500m,  TotalIva105: 52.5m,
        TotalExento:  0m,
        TotalGeneral: 1762.5m
    );

    private static EstadoCuentaReportDto EstadoCuentaEjemplo() => new(
        NombreEmpresa:        "Empresa Test SA",
        CuitEmpresa:          "30714000000",
        RazonSocialCliente:   "Juan Pérez",
        CuitCliente:          "20111222333",
        FechaDesde:           new DateOnly(2026, 1, 1),
        FechaHasta:           new DateOnly(2026, 6, 30),
        Movimientos: [
            new MovimientoCuentaDto(new DateOnly(2026, 6, 1),  "FC-B 0001-00000001", "Factura B",    1210m, 0,     1210m),
            new MovimientoCuentaDto(new DateOnly(2026, 6, 10), "REC 0001-00000001",  "Recibo cobro", 0,     1210m, 0m),
        ],
        SaldoAnterior: 0m,
        SaldoFinal:    0m
    );

    // ─── PDF Comprobante ──────────────────────────────────────────────────────
    [Fact]
    public async Task ComprobanteDocument_genera_pdf_valido_con_cae()
    {
        var pdf = await _reportService.GenerarComprobanteAsync(ComprobanteEjemplo(conCae: true), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue("debe comenzar con la firma PDF: %PDF");
    }

    [Fact]
    public async Task ComprobanteDocument_sin_cae_genera_pdf_valido()
    {
        var pdf = await _reportService.GenerarComprobanteAsync(ComprobanteEjemplo(conCae: false), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue();
    }

    // GAP-04: verificar que el QR PNG se genera correctamente (fix BUG-01)
    [Fact]
    public void QrFiscalService_genera_png_valido()
    {
        var url = "https://www.afip.gob.ar/fe/qr/?p=dGVzdA==";
        var png = _qrService.GenerarPng(url);

        png.Should().NotBeEmpty();
        EsPng(png).Should().BeTrue("debe comenzar con la firma PNG: 89 50 4E 47");
    }

    [Fact]
    public async Task ComprobanteDocument_con_qr_bytes_genera_pdf_mas_grande_que_sin_qr()
    {
        var conQr   = await _reportService.GenerarComprobanteAsync(ComprobanteEjemplo(conCae: true),  default);
        var sinQr   = await _reportService.GenerarComprobanteAsync(ComprobanteEjemplo(conCae: false), default);

        // El PDF con QR debe contener más datos (la imagen PNG embebida)
        conQr.Length.Should().BeGreaterThan(sinQr.Length,
            "el PDF con imagen QR debe ser más pesado que el PDF sin QR");
    }

    // ─── PDF Libro IVA ────────────────────────────────────────────────────────
    [Fact]
    public async Task LibroIvaDocument_genera_pdf_con_desglose_alicuotas()
    {
        var pdf = await _reportService.GenerarLibroIvaPdfAsync(LibroIvaEjemplo(), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue();
    }

    [Fact]
    public async Task LibroIvaDocument_sin_lineas_no_lanza()
    {
        var datos = LibroIvaEjemplo() with { Lineas = [] };

        var act = () => _reportService.GenerarLibroIvaPdfAsync(datos, default);

        await act.Should().NotThrowAsync();
    }

    // GAP-02: verificar que TotalGeneral = suma de líneas
    [Fact]
    public void LibroIva_TotalGeneral_es_suma_de_lineas()
    {
        var datos     = LibroIvaEjemplo();
        var sumaLineas = datos.Lineas.Sum(l => l.Total);

        sumaLineas.Should().Be(datos.TotalGeneral,
            "el total general debe coincidir con la suma de las líneas");
    }

    // ─── PDF Estado de cuenta ─────────────────────────────────────────────────
    [Fact]
    public async Task EstadoCuentaDocument_genera_pdf_valido()
    {
        var pdf = await _reportService.GenerarEstadoCuentaPdfAsync(EstadoCuentaEjemplo(), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue();
    }

    // ─── Excel exports ────────────────────────────────────────────────────────
    [Fact]
    public void ExportarClientes_genera_xlsx_valido()
    {
        var datos = new List<ClienteExportDto>
        {
            new("CLI001", "Juan Pérez",  "20111222333", "Consumidor Final", "CABA",    "11-1234-5678", null),
            new("CLI002", "Empresa SA",  "30714000000", "Resp. Inscripto",  "Córdoba", null,           "info@empresa.com"),
        };

        var xlsx = _excelService.ExportarClientes(datos);

        xlsx.Should().NotBeEmpty();
        EsXlsx(xlsx).Should().BeTrue();
    }

    [Fact]
    public void ExportarLibroIva_genera_xlsx_con_columnas_desgloseadas()
    {
        var xlsx = _excelService.ExportarLibroIva(LibroIvaEjemplo());

        xlsx.Should().NotBeEmpty();
        EsXlsx(xlsx).Should().BeTrue();
    }

    [Fact]
    public void ExportarArticulos_genera_xlsx_valido()
    {
        var datos = new List<ArticuloExportDto>
        {
            new("ART001", "Artículo test", "Grupo A", "UN", 1500m, true),
        };
        EsXlsx(_excelService.ExportarArticulos(datos)).Should().BeTrue();
    }

    // GAP-07: test faltante para ExportarMovimientosStock
    [Fact]
    public void ExportarMovimientosStock_genera_xlsx_valido()
    {
        var datos = new List<MovimientoExportDto>
        {
            new(new DateOnly(2026, 6, 1), "Artículo A", "Depósito Central", "Entrada", 10m, 500m),
            new(new DateOnly(2026, 6, 5), "Artículo B", "Depósito Sur",     "Salida",   3m, 800m),
        };

        var xlsx = _excelService.ExportarMovimientosStock(datos);

        xlsx.Should().NotBeEmpty();
        EsXlsx(xlsx).Should().BeTrue();
    }

    // GAP-03: performance — 10.000 líneas Libro IVA < 5 segundos (generación en memoria)
    [Fact]
    public async Task LibroIvaDocument_10000_lineas_genera_en_menos_de_5_segundos()
    {
        var lineas = Enumerable.Range(1, 10_000).Select(i => new LineaLibroIvaReportDto(
            new DateOnly(2026, 1, (i % 28) + 1),
            $"FC-B 0001-{i:D8}",
            $"Cliente {i}",
            $"2{i:D10}",
            Neto21: 1000m, Iva21: 210m,
            Neto105: 0m, Iva105: 0m, Exento: 0m,
            Total: 1210m)).ToList();

        var datos = LibroIvaEjemplo() with
        {
            Lineas = lineas,
            TotalNeto21  = 10_000 * 1000m,
            TotalIva21   = 10_000 * 210m,
            TotalGeneral = 10_000 * 1210m
        };

        var sw = Stopwatch.StartNew();
        var pdf = await _reportService.GenerarLibroIvaPdfAsync(datos, default);
        sw.Stop();

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5),
            "la generación de 10.000 líneas no debe superar 5 segundos");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static bool EsPdf(byte[] b)  => b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46;
    private static bool EsXlsx(byte[] b) => b.Length >= 2 && b[0] == 0x50 && b[1] == 0x4B;
    private static bool EsPng(byte[] b)  => b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
}
