namespace NewGest.Application.DTOs.Contabilidad;

public record LibroIvaDto(
    int Anio,
    int Mes,
    string TipoLibro,
    IReadOnlyList<LineaLibroIvaDto> Lineas,
    decimal TotalNeto,
    decimal TotalIva,
    decimal TotalGeneral
);

public record LineaLibroIvaDto(
    DateOnly Fecha,
    string TipoComprobante,
    int PuntoVenta,
    long Numero,
    string RazonSocial,
    string? Cuit,
    decimal TotalNeto,
    decimal TotalIva,
    decimal Total
);
