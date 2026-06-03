using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Entities.Inv;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Pedidos.Commands.GenerarRemito;

public class GenerarRemitoCommandHandler : IRequestHandler<GenerarRemitoCommand, RemitoDto>
{
    private readonly IPedidoRepository _pedidoRepo;
    private readonly IDepositoRepository _depositoRepo;
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _uow;

    public GenerarRemitoCommandHandler(
        IPedidoRepository pedidoRepo,
        IDepositoRepository depositoRepo,
        IStockService stockService,
        IUnitOfWork uow)
    {
        _pedidoRepo = pedidoRepo;
        _depositoRepo = depositoRepo;
        _stockService = stockService;
        _uow = uow;
    }

    public async Task<RemitoDto> Handle(GenerarRemitoCommand request, CancellationToken ct)
    {
        var pedido = await _pedidoRepo.GetByIdAsync(request.IdEmpresa, request.IdPedido, ct)
            ?? throw new DomainException($"Pedido {request.IdPedido} no encontrado.");

        var deposito = await _depositoRepo.GetByIdAsync(request.IdEmpresa, request.Datos.IdDeposito, ct)
            ?? throw new DomainException($"Depósito {request.Datos.IdDeposito} no encontrado.");

        var despachos = request.Datos.Items.Select(i => (i.IdArticulo, i.Cantidad));

        // Crea el remito y actualiza cantidades entregadas + estado del pedido en el dominio
        var remito = Remito.Crear(pedido, despachos);

        // Descontar stock por cada item del remito (sin su propia transacción — compartimos UoW)
        foreach (var item in remito.Items)
        {
            var mov = MovimientoStock.Crear(
                request.IdEmpresa,
                item.IdArticulo,
                deposito.IdDeposito,
                TipoMovimiento.Salida,
                item.Cantidad,
                costoUnitario: 0,  // en salidas el costo no se actualiza
                idComprobanteOrigen: null,
                observaciones: $"Remito generado desde pedido #{pedido.IdPedido}");

            await _stockService.RegistrarMovimientoAsync(mov, ct);
        }

        await _pedidoRepo.AddRemitoAsync(remito, ct);
        await _uow.CommitAsync(ct);

        return new RemitoDto(
            remito.IdRemito,
            remito.IdEmpresa,
            remito.IdPedido,
            remito.IdCliente,
            string.Empty,
            remito.FechaRemito,
            remito.Observaciones,
            remito.Items.Select(i => new ItemRemitoDto(
                i.IdItemRemito, i.IdArticulo, string.Empty, i.Cantidad, i.PrecioUnitario)).ToList());
    }
}
