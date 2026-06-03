using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Personal.Commands.DesactivarEmpleado;

public class DesactivarEmpleadoCommandHandler : IRequestHandler<DesactivarEmpleadoCommand>
{
    private readonly IEmpleadoRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesactivarEmpleadoCommandHandler(IEmpleadoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DesactivarEmpleadoCommand request, CancellationToken ct)
    {
        var empleado = await _repo.GetByIdAsync(request.IdEmpresa, request.IdEmpleado, ct)
            ?? throw new DomainException($"Empleado {request.IdEmpleado} no encontrado.");

        empleado.Desactivar();
        _repo.Update(empleado);
        await _uow.CommitAsync(ct);
    }
}
