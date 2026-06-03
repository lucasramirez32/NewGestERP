using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearEmpleado;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Personal.Queries.GetEmpleadoById;

public class GetEmpleadoByIdQueryHandler : IRequestHandler<GetEmpleadoByIdQuery, EmpleadoDto?>
{
    private readonly IEmpleadoRepository _repo;

    public GetEmpleadoByIdQueryHandler(IEmpleadoRepository repo)
    {
        _repo = repo;
    }

    public async Task<EmpleadoDto?> Handle(GetEmpleadoByIdQuery request, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(request.IdEmpresa, request.IdEmpleado, ct);
        return e is null ? null : CrearEmpleadoCommandHandler.ToDto(e);
    }
}
