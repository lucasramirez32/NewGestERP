using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Stock.Queries.GetAlertasReposicion;

public class GetAlertasReposicionQueryHandler : IRequestHandler<GetAlertasReposicionQuery, IReadOnlyList<ExistenciaDepositoDto>>
{
    private readonly IStockService _stockService;

    public GetAlertasReposicionQueryHandler(IStockService stockService)
    {
        _stockService = stockService;
    }

    public Task<IReadOnlyList<ExistenciaDepositoDto>> Handle(GetAlertasReposicionQuery request, CancellationToken ct)
        => _stockService.GetAlertasReposicionAsync(request.IdEmpresa, ct);
}
