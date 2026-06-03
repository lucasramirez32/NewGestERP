using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Articulos.Queries.GetGruposArticulos;

public class GetGruposArticulosQueryHandler : IRequestHandler<GetGruposArticulosQuery, IReadOnlyList<GrupoArticuloDto>>
{
    private readonly IGrupoArticuloRepository _repo;

    public GetGruposArticulosQueryHandler(IGrupoArticuloRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<GrupoArticuloDto>> Handle(GetGruposArticulosQuery request, CancellationToken cancellationToken)
        => _repo.GetArbolAsync(request.IdEmpresa, cancellationToken);
}
