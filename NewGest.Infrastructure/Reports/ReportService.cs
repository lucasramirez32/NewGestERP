using NewGest.Application.DTOs.Reportes;
using NewGest.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NewGest.Infrastructure.Reports;

public class ReportService : IReportService
{
    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Fix BUG-05: GeneratePdf() es CPU-bound. Task.Run() lo envía al ThreadPool
    // liberando el request thread. Bajo carga concurrente (lote de PDFs) evita
    // agotar los request threads de ASP.NET Core.
    public Task<byte[]> GenerarComprobanteAsync(ComprobanteReportDto datos, CancellationToken ct) =>
        Task.Run(() => new ComprobanteDocument(datos).GeneratePdf(), ct);

    public Task<byte[]> GenerarLibroIvaPdfAsync(LibroIvaReportDto datos, CancellationToken ct) =>
        Task.Run(() => new LibroIvaDocument(datos).GeneratePdf(), ct);

    public Task<byte[]> GenerarEstadoCuentaPdfAsync(EstadoCuentaReportDto datos, CancellationToken ct) =>
        Task.Run(() => new EstadoCuentaDocument(datos).GeneratePdf(), ct);
}
