using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Articulos.Commands.CrearArticulo;

public class CrearArticuloCommandHandler : IRequestHandler<CrearArticuloCommand, ArticuloDto>
{
    private readonly IArticuloRepository _repo;
    private readonly IGrupoArticuloRepository _grupoRepo;
    private readonly IUnitOfWork _uow;

    public CrearArticuloCommandHandler(
        IArticuloRepository repo,
        IGrupoArticuloRepository grupoRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _grupoRepo = grupoRepo;
        _uow = uow;
    }

    public async Task<ArticuloDto> Handle(CrearArticuloCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExisteCodigoAsync(request.IdEmpresa, request.Codigo, null, cancellationToken))
            throw new DomainException($"Ya existe un artículo con el código '{request.Codigo}'.");

        GrupoArticulo? grupo = null;

        if (!string.IsNullOrWhiteSpace(request.NombreGrupo))
        {
            var nombreTrimmed = request.NombreGrupo.Trim();
            grupo = await _grupoRepo.GetByDescripcionAsync(request.IdEmpresa, nombreTrimmed, cancellationToken);
            if (grupo == null)
            {
                grupo = new GrupoArticulo
                {
                    IdEmpresa = request.IdEmpresa,
                    Descripcion = nombreTrimmed
                };
                await _grupoRepo.AddAsync(grupo, cancellationToken);
            }
        }
        else if (request.IdGrupo.HasValue && request.IdGrupo.Value > 0)
        {
            grupo = await _grupoRepo.GetByIdAsync(request.IdEmpresa, request.IdGrupo.Value, cancellationToken)
                ?? throw new DomainException($"Grupo {request.IdGrupo} no encontrado.");
        }

        var articulo = Articulo.Crear(
            request.IdEmpresa,
            request.Codigo,
            request.Descripcion,
            grupo?.IdGrupo,
            request.IdUnidad,
            request.PrecioLista,
            request.PrecioCosto,
            request.PorcentajeIva,
            request.Observaciones);

        if (grupo != null)
        {
            articulo.AsociarGrupo(grupo);
        }

        await _repo.AddAsync(articulo, cancellationToken);
        await _uow.CommitAsync(cancellationToken);

        return ToDto(articulo, grupo?.Descripcion ?? string.Empty, string.Empty, string.Empty);
    }

    internal static ArticuloDto ToDto(Articulo a, string? grupoDescripcion, string unidadDescripcion, string unidadSimbolo)
        => new(
            a.IdArticulo,
            a.IdEmpresa,
            a.Codigo.Trim(),
            a.Descripcion,
            a.IdGrupo,
            grupoDescripcion,
            a.IdUnidad,
            unidadDescripcion,
            unidadSimbolo,
            a.PrecioLista,
            a.PrecioCosto,
            a.PorcentajeIva,
            a.Observaciones,
            a.Activo);
}
