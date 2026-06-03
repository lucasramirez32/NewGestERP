using MediatR;
using NewGest.Application.DTOs;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Auth.Queries.GetEmpresas;

public class GetEmpresasQueryHandler : IRequestHandler<GetEmpresasQuery, IReadOnlyList<EmpresaDto>>
{
    private readonly IEmpresaRepository _empresaRepo;

    public GetEmpresasQueryHandler(IEmpresaRepository empresaRepo)
    {
        _empresaRepo = empresaRepo;
    }

    public async Task<IReadOnlyList<EmpresaDto>> Handle(GetEmpresasQuery request, CancellationToken cancellationToken)
    {
        var empresas = await _empresaRepo.ObtenerActivasAsync(cancellationToken);
        return empresas.Select(e => new EmpresaDto(e.IdEmpresa, e.Nombre, e.Cuit)).ToList();
    }
}
