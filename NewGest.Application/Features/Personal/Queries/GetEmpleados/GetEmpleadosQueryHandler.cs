using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Personal.Queries.GetEmpleados;

public class GetEmpleadosQueryHandler : IRequestHandler<GetEmpleadosQuery, PagedResult<EmpleadoListItemDto>>
{
    private readonly IEmpleadoRepository _repo;

    public GetEmpleadosQueryHandler(IEmpleadoRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<EmpleadoListItemDto>> Handle(GetEmpleadosQuery request, CancellationToken ct)
        => _repo.GetPagedAsync(request.IdEmpresa, request.Search, request.SoloVendedores, request.Page, request.PageSize, ct);
}
