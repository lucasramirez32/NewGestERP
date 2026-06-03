using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Personal.Commands.CrearEmpleado;

public class CrearEmpleadoCommandHandler : IRequestHandler<CrearEmpleadoCommand, EmpleadoDto>
{
    private readonly IEmpleadoRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearEmpleadoCommandHandler(IEmpleadoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<EmpleadoDto> Handle(CrearEmpleadoCommand request, CancellationToken ct)
    {
        var dto = request.Datos;

        if (await _repo.ExisteLegajoAsync(request.IdEmpresa, dto.Legajo, null, ct))
            throw new DomainException($"Ya existe un empleado con el legajo '{dto.Legajo}'.");

        var empleado = Empleado.Crear(
            request.IdEmpresa,
            dto.Legajo,
            dto.ApellidoNombre,
            dto.CUIL,
            dto.Rol,
            dto.ComisionPorcentaje);

        await _repo.AddAsync(empleado, ct);
        await _uow.CommitAsync(ct);

        return ToDto(empleado);
    }

    internal static EmpleadoDto ToDto(Empleado e) => new(
        e.IdEmpleado,
        e.IdEmpresa,
        e.Legajo,
        e.ApellidoNombre,
        e.CUIL,
        e.Rol,
        e.Rol.ToString(),
        e.EsVendedor,
        e.ComisionPorcentaje,
        e.Activo);
}
