using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Stock.Commands.RegistrarMovimiento;

public record RegistrarMovimientoCommand(
    int IdEmpresa,
    int IdArticulo,
    int IdDeposito,
    TipoMovimiento Tipo,
    decimal Cantidad,
    decimal CostoUnitario,
    string? NumeroSerie,
    int? IdComprobanteOrigen,
    string? Observaciones) : IRequest<MovimientoStockDto>;
