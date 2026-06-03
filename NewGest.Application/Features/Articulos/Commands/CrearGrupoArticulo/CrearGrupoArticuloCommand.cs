using MediatR;
using NewGest.Application.DTOs.Articulos;

namespace NewGest.Application.Features.Articulos.Commands.CrearGrupoArticulo;

public record CrearGrupoArticuloCommand(
    int IdEmpresa,
    string Descripcion,
    int? IdGrupoPadre
) : IRequest<GrupoArticuloDto>;
