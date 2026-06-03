using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Application.Features.Stock.Commands.RegistrarMovimiento;

public class RegistrarMovimientoCommandHandler : IRequestHandler<RegistrarMovimientoCommand, MovimientoStockDto>
{
    private readonly IStockService _stockService;
    private readonly IDepositoRepository _depositoRepo;
    private readonly IUnitOfWork _uow;

    public RegistrarMovimientoCommandHandler(
        IStockService stockService,
        IDepositoRepository depositoRepo,
        IUnitOfWork uow)
    {
        _stockService = stockService;
        _depositoRepo = depositoRepo;
        _uow = uow;
    }

    public async Task<MovimientoStockDto> Handle(RegistrarMovimientoCommand request, CancellationToken ct)
    {
        var deposito = await _depositoRepo.GetByIdAsync(request.IdEmpresa, request.IdDeposito, ct)
            ?? throw new Domain.Common.DomainException($"Depósito {request.IdDeposito} no encontrado.");

        var mov = MovimientoStock.Crear(
            request.IdEmpresa,
            request.IdArticulo,
            request.IdDeposito,
            request.Tipo,
            request.Cantidad,
            request.CostoUnitario,
            request.NumeroSerie,
            request.IdComprobanteOrigen,
            request.Observaciones);

        await _stockService.RegistrarMovimientoAsync(mov, ct);
        await _uow.CommitAsync(ct);

        return new MovimientoStockDto(
            mov.IdMovimiento,
            mov.IdArticulo,
            string.Empty,       // sin join — el handler no carga nav properties
            mov.IdDeposito,
            deposito.Descripcion,
            mov.Tipo,
            mov.Tipo.ToString(),
            mov.Cantidad,
            mov.CostoUnitario,
            mov.NumeroSerie,
            mov.IdComprobanteOrigen,
            mov.FechaMovimiento,
            mov.Observaciones);
    }
}
