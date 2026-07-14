using NewGest.Application.DTOs.Reportes;

namespace NewGest.Application.Services;

public interface IReportService
{
    Task<byte[]> GenerarComprobanteAsync(ComprobanteReportDto datos, CancellationToken ct);
    Task<byte[]> GenerarLibroIvaPdfAsync(LibroIvaReportDto datos, CancellationToken ct);
    Task<byte[]> GenerarEstadoCuentaPdfAsync(EstadoCuentaReportDto datos, CancellationToken ct);
}

public interface IExcelExportService
{
    byte[] ExportarClientes(IEnumerable<ClienteExportDto> datos);
    byte[] ExportarArticulos(IEnumerable<ArticuloExportDto> datos);
    byte[] ExportarMovimientosStock(IEnumerable<MovimientoExportDto> datos);
    byte[] ExportarLibroIva(LibroIvaReportDto datos);
}
