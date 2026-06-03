using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Stock.Queries.GetExistencias;

public class GetExistenciasQueryHandler : IRequestHandler<GetExistenciasQuery, IReadOnlyList<ExistenciaDepositoDto>>
{
    private readonly IStockService _stockService;

    public GetExistenciasQueryHandler(IStockService stockService)
    {
        _stockService = stockService;
    }

    public Task<IReadOnlyList<ExistenciaDepositoDto>> Handle(GetExistenciasQuery request, CancellationToken ct)
        => _stockService.GetExistenciasAsync(request.IdEmpresa, request.IdArticulo, ct);
}
