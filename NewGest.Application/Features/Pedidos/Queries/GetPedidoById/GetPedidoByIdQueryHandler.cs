using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.Features.Pedidos.Commands.CrearPedido;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Pedidos.Queries.GetPedidoById;

public class GetPedidoByIdQueryHandler : IRequestHandler<GetPedidoByIdQuery, PedidoDto?>
{
    private readonly IPedidoRepository _repo;

    public GetPedidoByIdQueryHandler(IPedidoRepository repo)
    {
        _repo = repo;
    }

    public async Task<PedidoDto?> Handle(GetPedidoByIdQuery request, CancellationToken ct)
    {
        var pedido = await _repo.GetByIdAsync(request.IdEmpresa, request.IdPedido, ct);
        return pedido is null ? null : CrearPedidoCommandHandler.ToDto(pedido);
    }
}
