using MediatR;
using NewGest.Application.DTOs.Articulos;

namespace NewGest.Application.Features.Articulos.Queries.GetArticuloById;

public record GetArticuloByIdQuery(int IdEmpresa, int IdArticulo) : IRequest<ArticuloDto?>;
