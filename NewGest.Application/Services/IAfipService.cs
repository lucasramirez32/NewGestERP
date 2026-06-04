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

/// <summary>
/// Desglose por alícuota para enviar a AFIP (id = código AFIP, BaseImp = neto, Importe = IVA).
/// Códigos AFIP: Exento=3, 0%=3, 2.5%=9, 5%=8, 10.5%=4, 21%=5, 27%=6
/// </summary>
public record AlicuotaAfipDetalle(int IdAfip, decimal BaseImponible, decimal Importe);

public record ComprobanteAfip(
    string CuitEmisor,
    TipoComprobante Tipo,
    int PuntoVenta,
    long NumeroDesde,
    long NumeroHasta,
    DateOnly FechaComprobante,
    string? CuitReceptor,
    decimal TotalExento,
    decimal TotalComprobante,
    IReadOnlyList<AlicuotaAfipDetalle> Alicuotas
);
