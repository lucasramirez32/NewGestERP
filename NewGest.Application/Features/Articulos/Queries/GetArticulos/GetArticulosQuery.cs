using MediatR;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;

namespace NewGest.Application.Features.Articulos.Queries.GetArticulos;

public record GetArticulosQuery(
    int IdEmpresa,
    string? Search,
    int? IdGrupo,
    int Page,
    int PageSize
) : IRequest<PagedResult<ArticuloListItemDto>>;
