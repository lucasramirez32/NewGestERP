using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Clientes.Queries.GetClientes;

public class GetClientesQueryHandler : IRequestHandler<GetClientesQuery, PagedResult<ClienteListItemDto>>
{
    private readonly IClienteRepository _repo;

    public GetClientesQueryHandler(IClienteRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<ClienteListItemDto>> Handle(GetClientesQuery request, CancellationToken cancellationToken)
        => _repo.GetPagedAsync(
            request.IdEmpresa,
            request.Search,
            request.CondicionIva,
            request.IdZona,
            request.Page,
            request.PageSize,
            cancellationToken);
}
