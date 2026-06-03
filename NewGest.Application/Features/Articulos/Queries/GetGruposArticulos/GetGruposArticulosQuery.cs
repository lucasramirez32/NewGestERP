using MediatR;
using NewGest.Application.DTOs.Articulos;

namespace NewGest.Application.Features.Articulos.Queries.GetGruposArticulos;

public record GetGruposArticulosQuery(int IdEmpresa) : IRequest<IReadOnlyList<GrupoArticuloDto>>;
