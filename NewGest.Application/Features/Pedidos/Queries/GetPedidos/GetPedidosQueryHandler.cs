using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Pedidos.Queries.GetPedidos;

public class GetPedidosQueryHandler : IRequestHandler<GetPedidosQuery, PagedResult<PedidoListItemDto>>
{
    private readonly IPedidoRepository _repo;

    public GetPedidosQueryHandler(IPedidoRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<PedidoListItemDto>> Handle(GetPedidosQuery request, CancellationToken ct)
        => _repo.GetPagedAsync(request.IdEmpresa, request.Estado, request.Page, request.PageSize, ct);
}
