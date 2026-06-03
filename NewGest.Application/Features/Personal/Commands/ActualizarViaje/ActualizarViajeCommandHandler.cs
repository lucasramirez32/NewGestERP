using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearViaje;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Personal.Commands.ActualizarViaje;

public class ActualizarViajeCommandHandler : IRequestHandler<ActualizarViajeCommand, ViajeDto>
{
    private readonly IViajeRepository _repo;
    private readonly IUnitOfWork _uow;

    public ActualizarViajeCommandHandler(IViajeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ViajeDto> Handle(ActualizarViajeCommand request, CancellationToken ct)
    {
        var viaje = await _repo.GetByIdAsync(request.IdEmpresa, request.IdViaje, ct)
            ?? throw new DomainException($"Viaje {request.IdViaje} no encontrado.");

        var dto = request.Datos;
        viaje.Descripcion = dto.Descripcion.Trim();
        viaje.FechaViaje = dto.FechaViaje;
        viaje.Destino = dto.Destino?.Trim();
        viaje.Observaciones = dto.Observaciones?.Trim();

        _repo.Update(viaje);
        await _uow.CommitAsync(ct);

        return CrearViajeCommandHandler.ToDto(viaje);
    }
}
