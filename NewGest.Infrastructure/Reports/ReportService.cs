using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

public class ReportService : IReportService
{
    static ReportService()
    {
        // Licencia Community (gratuita para proyectos open source / internos)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerarComprobanteAsync(ComprobanteReportDto datos, CancellationToken ct)
    {
        var pdf = new ComprobanteDocument(datos).GeneratePdf();
        return Task.FromResult(pdf);
    }

    public Task<byte[]> GenerarLibroIvaPdfAsync(LibroIvaReportDto datos, CancellationToken ct)
    {
        var pdf = new LibroIvaDocument(datos).GeneratePdf();
        return Task.FromResult(pdf);
    }

    public Task<byte[]> GenerarEstadoCuentaPdfAsync(EstadoCuentaReportDto datos, CancellationToken ct)
    {
        var pdf = new EstadoCuentaDocument(datos).GeneratePdf();
        return Task.FromResult(pdf);
    }
}
