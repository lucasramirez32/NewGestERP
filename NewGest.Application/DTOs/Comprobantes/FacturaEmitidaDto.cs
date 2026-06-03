namespace NewGest.Application.DTOs.Comprobantes;

public record FacturaEmitidaDto(
    int IdComprobante,
    string Tipo,
    int PuntoVenta,
    long Numero,
    string? CodigoCae,
    DateOnly? FechaVencimientoCae,
    decimal Total
);
