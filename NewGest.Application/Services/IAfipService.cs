using NewGest.Domain.Enums;

namespace NewGest.Application.Services;

public interface IAfipService
{
    Task<CaeResponse> SolicitarCaeAsync(ComprobanteAfip comprobante, CancellationToken ct);
    Task<bool> ValidarComprobanteAsync(string cuit, TipoComprobante tipo, long numero, CancellationToken ct);
    Task<PuntoVentaAfip[]> ObtenerPuntosVentaAsync(string cuit, CancellationToken ct);
}

public record CaeResponse(string CodigoCae, DateOnly FechaVencimiento, string NumeroComprobante);

public record PuntoVentaAfip(int Numero, string Descripcion, bool Bloqueado);

public record ComprobanteAfip(
    string CuitEmisor,
    TipoComprobante Tipo,
    int PuntoVenta,
    long NumeroDesde,
    long NumeroHasta,
    DateOnly FechaComprobante,
    string? CuitReceptor,
    decimal TotalNeto21,
    decimal Iva21,
    decimal TotalNeto105,
    decimal Iva105,
    decimal TotalExento,
    decimal TotalComprobante
);
