using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Stock.Queries.GetDepositos;

public class GetDepositosQueryHandler : IRequestHandler<GetDepositosQuery, IReadOnlyList<DepositoDto>>
{
    private readonly IDepositoRepository _repo;

    public GetDepositosQueryHandler(IDepositoRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<DepositoDto>> Handle(GetDepositosQuery request, CancellationToken ct)
    {
        var depositos = await _repo.GetByEmpresaAsync(request.IdEmpresa, ct);
        return depositos.Select(d => new DepositoDto(d.IdDeposito, d.Codigo, d.Descripcion, d.Activo))
                        .ToList();
    }
}
