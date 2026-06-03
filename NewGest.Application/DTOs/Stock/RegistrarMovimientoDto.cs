using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Stock;

public record RegistrarMovimientoDto(
    int IdArticulo,
    int IdDeposito,
    TipoMovimiento Tipo,
    decimal Cantidad,
    decimal CostoUnitario,
    string? NumeroSerie,
    int? IdComprobanteOrigen,
    string? Observaciones);
