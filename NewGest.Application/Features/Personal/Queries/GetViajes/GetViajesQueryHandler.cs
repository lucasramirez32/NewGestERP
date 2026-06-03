using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearViaje;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Personal.Queries.GetViajes;

public class GetViajesQueryHandler : IRequestHandler<GetViajesQuery, IReadOnlyList<ViajeDto>>
{
    private readonly IViajeRepository _repo;

    public GetViajesQueryHandler(IViajeRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<ViajeDto>> Handle(GetViajesQuery request, CancellationToken ct)
    {
        var viajes = await _repo.GetByEmpresaAsync(request.IdEmpresa, ct);
        return viajes.Select(CrearViajeCommandHandler.ToDto).ToList();
    }
}
