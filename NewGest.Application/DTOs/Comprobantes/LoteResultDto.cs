namespace NewGest.Application.DTOs.Comprobantes;

public record LoteResultDto(
    int TotalEnviados,
    int TotalExitosos,
    int TotalErrores,
    List<LoteItemResultDto> Resultados
);

public record LoteItemResultDto(
    int Indice,
    bool Exito,
    FacturaEmitidaDto? Factura,
    string? Error
);
