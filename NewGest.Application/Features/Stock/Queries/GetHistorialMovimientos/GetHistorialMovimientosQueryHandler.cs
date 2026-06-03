using MediatR;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Stock.Queries.GetHistorialMovimientos;

public class GetHistorialMovimientosQueryHandler
    : IRequestHandler<GetHistorialMovimientosQuery, PagedResult<MovimientoStockDto>>
{
    private readonly IStockService _stockService;

    public GetHistorialMovimientosQueryHandler(IStockService stockService)
    {
        _stockService = stockService;
    }

    public Task<PagedResult<MovimientoStockDto>> Handle(
        GetHistorialMovimientosQuery request, CancellationToken ct)
        => _stockService.GetHistorialAsync(
            request.IdEmpresa, request.IdArticulo, request.Page, request.PageSize, ct);
}
