using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Features.Articulos.Commands.CrearArticulo;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Articulos.Queries.GetArticuloById;

public class GetArticuloByIdQueryHandler : IRequestHandler<GetArticuloByIdQuery, ArticuloDto?>
{
    private readonly IArticuloRepository _repo;

    public GetArticuloByIdQueryHandler(IArticuloRepository repo)
    {
        _repo = repo;
    }

    public async Task<ArticuloDto?> Handle(GetArticuloByIdQuery request, CancellationToken cancellationToken)
    {
        var articulo = await _repo.GetByIdAsync(request.IdEmpresa, request.IdArticulo, cancellationToken);
        if (articulo is null) return null;

        return CrearArticuloCommandHandler.ToDto(
            articulo,
            articulo.Grupo?.Descripcion ?? string.Empty,
            articulo.Unidad?.Descripcion ?? string.Empty,
            articulo.Unidad?.Simbolo ?? string.Empty);
    }
}
