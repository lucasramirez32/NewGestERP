using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Pedidos.Commands.AnularPedido;

public class AnularPedidoCommandHandler : IRequestHandler<AnularPedidoCommand>
{
    private readonly IPedidoRepository _repo;
    private readonly IUnitOfWork _uow;

    public AnularPedidoCommandHandler(IPedidoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(AnularPedidoCommand request, CancellationToken ct)
    {
        var pedido = await _repo.GetByIdAsync(request.IdEmpresa, request.IdPedido, ct)
            ?? throw new DomainException($"Pedido {request.IdPedido} no encontrado.");

        pedido.Anular();
        await _uow.CommitAsync(ct);
    }
}
