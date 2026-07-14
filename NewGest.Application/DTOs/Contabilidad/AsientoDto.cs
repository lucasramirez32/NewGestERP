namespace NewGest.Application.DTOs.Contabilidad;

public record AsientoDto(
    int IdAsiento,
    long Numero,
    DateOnly Fecha,
    string Descripcion,
    string TipoAsiento,
    int? IdComprobanteOrigen,
    decimal TotalDebe,
    decimal TotalHaber,
    bool Anulado,
    IReadOnlyList<PartidaAsientoDto> Partidas
);

public record PartidaAsientoDto(
    int IdCuenta,
    string CodigoCuenta,
    string DescripcionCuenta,
    decimal Debe,
    decimal Haber,
    string? Concepto
);

public record CrearAsientoDto(
    DateOnly Fecha,
    string Descripcion,
    List<CrearPartidaDto> Partidas
);

public record CrearPartidaDto(
    int IdCuenta,
    decimal Debe,
    decimal Haber,
    string? Concepto
);
