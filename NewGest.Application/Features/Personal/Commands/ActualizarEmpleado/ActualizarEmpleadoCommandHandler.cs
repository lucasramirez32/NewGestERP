using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearEmpleado;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Personal.Commands.ActualizarEmpleado;

public class ActualizarEmpleadoCommandHandler : IRequestHandler<ActualizarEmpleadoCommand, EmpleadoDto>
{
    private readonly IEmpleadoRepository _repo;
    private readonly IUnitOfWork _uow;

    public ActualizarEmpleadoCommandHandler(IEmpleadoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EmpleadoDto> Handle(ActualizarEmpleadoCommand request, CancellationToken ct)
    {
        var empleado = await _repo.GetByIdAsync(request.IdEmpresa, request.IdEmpleado, ct)
            ?? throw new DomainException($"Empleado {request.IdEmpleado} no encontrado.");

        var dto = request.Datos;
        empleado.Actualizar(dto.ApellidoNombre, dto.CUIL, dto.Rol, dto.ComisionPorcentaje);
        _repo.Update(empleado);
        await _uow.CommitAsync(ct);

        return CrearEmpleadoCommandHandler.ToDto(empleado);
    }
}
