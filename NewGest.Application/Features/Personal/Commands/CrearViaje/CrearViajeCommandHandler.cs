using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Personal.Commands.CrearViaje;

public class CrearViajeCommandHandler : IRequestHandler<CrearViajeCommand, ViajeDto>
{
    private readonly IViajeRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearViajeCommandHandler(IViajeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ViajeDto> Handle(CrearViajeCommand request, CancellationToken ct)
    {
        var dto = request.Datos;
        var viaje = new Viaje
        {
            IdEmpresa = request.IdEmpresa,
            Descripcion = dto.Descripcion.Trim(),
            FechaViaje = dto.FechaViaje,
            Destino = dto.Destino?.Trim(),
            Observaciones = dto.Observaciones?.Trim(),
            Activo = true
        };

        await _repo.AddAsync(viaje, ct);
        await _uow.CommitAsync(ct);

        return ToDto(viaje);
    }

    internal static ViajeDto ToDto(Viaje v) =>
        new(v.IdViaje, v.Descripcion, v.FechaViaje, v.Destino, v.Observaciones, v.Activo);
}
