using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearMutual;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Personal.Queries.GetMutuales;

public class GetMutualesQueryHandler : IRequestHandler<GetMutualesQuery, IReadOnlyList<MutualDto>>
{
    private readonly IMutualRepository _repo;

    public GetMutualesQueryHandler(IMutualRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<MutualDto>> Handle(GetMutualesQuery request, CancellationToken ct)
    {
        var mutuales = await _repo.GetByEmpresaAsync(request.IdEmpresa, ct);
        return mutuales.Select(CrearMutualCommandHandler.ToDto).ToList();
    }
}
