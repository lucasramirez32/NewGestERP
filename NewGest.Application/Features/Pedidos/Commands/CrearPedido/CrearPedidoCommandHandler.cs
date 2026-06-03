using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Com;

namespace NewGest.Application.Features.Pedidos.Commands.CrearPedido;

public class CrearPedidoCommandHandler : IRequestHandler<CrearPedidoCommand, PedidoDto>
{
    private readonly IPedidoRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearPedidoCommandHandler(IPedidoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<PedidoDto> Handle(CrearPedidoCommand request, CancellationToken ct)
    {
        var dto = request.Datos;

        var pedido = Pedido.Crear(
            request.IdEmpresa,
            dto.IdCliente,
            dto.IdVendedor,
            dto.FechaEntregaEstimada,
            dto.Observaciones,
            dto.Items.Select(i => (i.IdArticulo, i.Cantidad, i.PrecioUnitario)));

        await _repo.AddAsync(pedido, ct);
        await _uow.CommitAsync(ct);

        return ToDto(pedido);
    }

    internal static PedidoDto ToDto(Pedido p) => new(
        p.IdPedido,
        p.IdEmpresa,
        p.IdCliente,
        string.Empty,
        p.IdVendedor,
        null,
        p.Estado,
        p.Estado.ToString(),
        p.FechaPedido,
        p.FechaEntregaEstimada,
        p.Observaciones,
        p.Items.Select(i => new ItemPedidoDto(
            i.IdItemPedido, i.IdArticulo, string.Empty,
            i.CantidadPedida, i.CantidadEntregada, i.CantidadPendiente,
            i.PrecioUnitario)).ToList());
}
