using FluentAssertions;
using NewGest.Application.DTOs.Reportes;
using NewGest.Infrastructure.Reports;

namespace NewGest.IntegrationTests.Reportes;

/// <summary>
/// Tests unitarios para los generadores de reportes PDF y Excel.
/// No usan base de datos — verifican que los documentos se generan sin errores
/// y que los bytes producidos tienen los magic numbers correctos.
/// </summary>
public class ReportesTests
{
    private static readonly ReportService _reportService = new();
    private static readonly ExcelExportService _excelService = new();

    // ─── Fixtures de datos de prueba ──────────────────────────────────────────
    private static ComprobanteReportDto ComprobanteEjemplo() => new(
        NombreEmpresa: "Empresa Test SA",
        RazonSocialEmpresa: "Empresa Test S.A.",
        CuitEmpresa: "30714000000",
        CondicionIvaEmpresa: "Responsable Inscripto",
        DomicilioEmpresa: "Av. Córdoba 1234, CABA",
        TipoLabel: "FACTURA B",
        PuntoVenta: 1,
        Numero: 42,
        Fecha: new DateOnly(2026, 6, 5),
        RazonSocialCliente: "Juan Pérez",
        CuitCliente: "20111222333",
        CondicionIvaCliente: "Consumidor Final",
        DomicilioCliente: null,
        Items: [
            new ItemReportDto("Servicio de prueba", 2m, 500m, "21%", 1000m),
            new ItemReportDto("Otro servicio",      1m, 200m, "21%",  200m),
        ],
        Alicuotas: [new AlicuotaReportDto("21%", 1200m, 252m)],
        TotalNeto: 1200m,
        TotalIva:  252m,
        Total:     1452m,
        CodigoCae: "12345678901234",
        VencimientoCae: new DateOnly(2026, 8, 31),
        QrUrl: "https://www.afip.gob.ar/fe/qr/?p=dGVzdA=="
    );

    private static LibroIvaReportDto LibroIvaEjemplo() => new(
        NombreEmpresa: "Empresa Test SA",
        CuitEmpresa: "30714000000",
        Anio: 2026, Mes: 6,
        TipoLibro: "Ventas",
        Lineas: [
            new LineaLibroIvaReportDto(new DateOnly(2026, 6, 1), "FC-B 0001-00000001",
                "Cliente A", "20111222333", 1000m, 210m, 1210m),
            new LineaLibroIvaReportDto(new DateOnly(2026, 6, 5), "FC-B 0001-00000002",
                "Cliente B", "20999888777", 500m, 105m, 605m),
        ],
        TotalNeto: 1500m, TotalIva: 315m, TotalGeneral: 1815m
    );

    private static EstadoCuentaReportDto EstadoCuentaEjemplo() => new(
        NombreEmpresa: "Empresa Test SA",
        CuitEmpresa: "30714000000",
        RazonSocialCliente: "Juan Pérez",
        CuitCliente: "20111222333",
        FechaDesde: new DateOnly(2026, 1, 1),
        FechaHasta: new DateOnly(2026, 6, 30),
        Movimientos: [
            new MovimientoCuentaDto(new DateOnly(2026, 6, 1), "FC-B 0001-00000001",
                "Factura B", 1210m, 0, 1210m),
            new MovimientoCuentaDto(new DateOnly(2026, 6, 10), "REC 0001-00000001",
                "Recibo cobro", 0, 1210m, 0m),
        ],
        SaldoAnterior: 0m,
        SaldoFinal:    0m
    );

    // ─── PDF Comprobante ──────────────────────────────────────────────────────
    [Fact]
    public async Task ComprobanteDocument_genera_pdf_no_vacio()
    {
        var pdf = await _reportService.GenerarComprobanteAsync(ComprobanteEjemplo(), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue("debe comenzar con la firma PDF: %PDF");
    }

    [Fact]
    public async Task ComprobanteDocument_sin_cae_genera_pdf_sin_lanzar()
    {
        var datos = ComprobanteEjemplo() with { CodigoCae = null, VencimientoCae = null, QrUrl = null };

        var act = () => _reportService.GenerarComprobanteAsync(datos, default);

        await act.Should().NotThrowAsync();
    }

    // ─── PDF Libro IVA ────────────────────────────────────────────────────────
    [Fact]
    public async Task LibroIvaDocument_genera_pdf_con_totales()
    {
        var pdf = await _reportService.GenerarLibroIvaPdfAsync(LibroIvaEjemplo(), default);

        pdf.Should().NotBeEmpty();
        EsPdf(pdf).Should().BeTrue();
    }

    [Fact]
    public async Task LibroIvaDocument_sin_lineas_genera_pdf_sin_lanzar()
    {
        var datos = LibroIvaEjemplo() with { Lineas = [] };

        var act = () => _reportService.GenerarLibroIvaPdfAsync(datos, default);

        await act.Should().NotThrowAsync();
    }

    // ─── PDF Estado de cuenta ─────────────────────────────────────────────────
    [Fact]
    public async Task EstadoCuentaDocument_genera_pdf_no_vacio()
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
            new("CLI001", "Juan Pérez", "20111222333", "Consumidor Final", "CABA", "11-1234-5678", null),
            new("CLI002", "Empresa SA", "30714000000", "Resp. Inscripto", "Córdoba", null, "info@empresa.com"),
        };

        var xlsx = _excelService.ExportarClientes(datos);

        xlsx.Should().NotBeEmpty();
        EsXlsx(xlsx).Should().BeTrue("debe comenzar con la firma ZIP/XLSX: PK");
    }

    [Fact]
    public void ExportarLibroIva_genera_xlsx_con_totales()
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

        var xlsx = _excelService.ExportarArticulos(datos);

        xlsx.Should().NotBeEmpty();
        EsXlsx(xlsx).Should().BeTrue();
    }

    // ─── Helpers de verificación de formato ──────────────────────────────────
    private static bool EsPdf(byte[] bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == 0x25 && bytes[1] == 0x50 &&
        bytes[2] == 0x44 && bytes[3] == 0x46; // %PDF

    private static bool EsXlsx(byte[] bytes) =>
        bytes.Length >= 2 &&
        bytes[0] == 0x50 && bytes[1] == 0x4B; // PK (ZIP)
}
