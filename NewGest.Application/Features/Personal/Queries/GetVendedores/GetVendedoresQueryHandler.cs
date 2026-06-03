using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Personal.Queries.GetVendedores;

public class GetVendedoresQueryHandler : IRequestHandler<GetVendedoresQuery, IReadOnlyList<EmpleadoListItemDto>>
{
    private readonly IEmpleadoRepository _repo;

    public GetVendedoresQueryHandler(IEmpleadoRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<EmpleadoListItemDto>> Handle(GetVendedoresQuery request, CancellationToken ct)
    {
        var result = await _repo.GetPagedAsync(request.IdEmpresa, null, soloVendedores: true, 1, 500, ct);
        return result.Items;
    }
}
