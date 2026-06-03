namespace NewGest.Application.DTOs.Comprobantes;

public record ComprobanteDto(
    int IdComprobante,
    string Tipo,
    int PuntoVenta,
    long Numero,
    DateOnly Fecha,
    string RazonSocialCliente,
    string? CuitCliente,
    string CondicionIvaReceptor,
    decimal TotalNeto,
    decimal TotalIva,
    decimal Total,
    string? CodigoCae,
    DateOnly? FechaVencimientoCae,
    bool EsElectronica,
    bool Anulado,
    IReadOnlyList<ItemComprobanteDto> Items
);
