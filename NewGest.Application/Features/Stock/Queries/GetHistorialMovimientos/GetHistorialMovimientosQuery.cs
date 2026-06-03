using MediatR;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.DTOs.Stock;

namespace NewGest.Application.Features.Stock.Queries.GetHistorialMovimientos;

public record GetHistorialMovimientosQuery(
    int IdEmpresa,
    int IdArticulo,
    int Page,
    int PageSize) : IRequest<PagedResult<MovimientoStockDto>>;
