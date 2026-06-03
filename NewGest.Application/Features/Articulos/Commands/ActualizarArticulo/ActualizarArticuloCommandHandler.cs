using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Features.Articulos.Commands.CrearArticulo;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Articulos.Commands.ActualizarArticulo;

public class ActualizarArticuloCommandHandler : IRequestHandler<ActualizarArticuloCommand, ArticuloDto>
{
    private readonly IArticuloRepository _repo;
    private readonly IGrupoArticuloRepository _grupoRepo;
    private readonly IUnitOfWork _uow;

    public ActualizarArticuloCommandHandler(
        IArticuloRepository repo,
        IGrupoArticuloRepository grupoRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _grupoRepo = grupoRepo;
        _uow = uow;
    }

    public async Task<ArticuloDto> Handle(ActualizarArticuloCommand request, CancellationToken cancellationToken)
    {
        var articulo = await _repo.GetByIdAsync(request.IdEmpresa, request.IdArticulo, cancellationToken)
            ?? throw new DomainException($"Artículo {request.IdArticulo} no encontrado.");

        var grupo = await _grupoRepo.GetByIdAsync(request.IdEmpresa, request.IdGrupo, cancellationToken)
            ?? throw new DomainException($"Grupo {request.IdGrupo} no encontrado.");

        articulo.Actualizar(
            request.Descripcion,
            request.IdGrupo,
            request.IdUnidad,
            request.PrecioLista,
            request.PrecioCosto,
            request.PorcentajeIva,
            request.Observaciones);

        _repo.Update(articulo);
        await _uow.CommitAsync(cancellationToken);

        // Recargar unidad desde la navegación si está disponible
        var unidadDesc = articulo.Unidad?.Descripcion ?? string.Empty;
        var unidadSimb = articulo.Unidad?.Simbolo ?? string.Empty;

        return CrearArticuloCommandHandler.ToDto(articulo, grupo.Descripcion, unidadDesc, unidadSimb);
    }
}
