using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Stock;

public record MovimientoStockDto(
    int IdMovimiento,
    int IdArticulo,
    string DescripcionArticulo,
    int IdDeposito,
    string NombreDeposito,
    TipoMovimiento Tipo,
    string TipoDescripcion,
    decimal Cantidad,
    decimal CostoUnitario,
    string? NumeroSerie,
    int? IdComprobanteOrigen,
    DateTime FechaMovimiento,
    string? Observaciones);
