using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Articulos.Queries.GetArticulos;

public class GetArticulosQueryHandler : IRequestHandler<GetArticulosQuery, PagedResult<ArticuloListItemDto>>
{
    private readonly IArticuloRepository _repo;

    public GetArticulosQueryHandler(IArticuloRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<ArticuloListItemDto>> Handle(GetArticulosQuery request, CancellationToken cancellationToken)
        => _repo.GetPagedAsync(
            request.IdEmpresa,
            request.Search,
            request.IdGrupo,
            request.Page,
            request.PageSize,
            cancellationToken);
}
