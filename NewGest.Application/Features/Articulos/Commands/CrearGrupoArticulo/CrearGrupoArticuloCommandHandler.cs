using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Articulos.Commands.CrearGrupoArticulo;

public class CrearGrupoArticuloCommandHandler : IRequestHandler<CrearGrupoArticuloCommand, GrupoArticuloDto>
{
    private readonly IGrupoArticuloRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearGrupoArticuloCommandHandler(IGrupoArticuloRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<GrupoArticuloDto> Handle(CrearGrupoArticuloCommand request, CancellationToken cancellationToken)
    {
        if (request.IdGrupoPadre.HasValue)
        {
            var padre = await _repo.GetByIdAsync(request.IdEmpresa, request.IdGrupoPadre.Value, cancellationToken)
                ?? throw new DomainException($"Grupo padre {request.IdGrupoPadre} no encontrado.");
        }

        var grupo = new GrupoArticulo
        {
            IdEmpresa = request.IdEmpresa,
            Descripcion = request.Descripcion.Trim(),
            IdGrupoPadre = request.IdGrupoPadre
        };

        await _repo.AddAsync(grupo, cancellationToken);
        await _uow.CommitAsync(cancellationToken);

        return new GrupoArticuloDto(grupo.IdGrupo, grupo.Descripcion, grupo.IdGrupoPadre, []);
    }
}
