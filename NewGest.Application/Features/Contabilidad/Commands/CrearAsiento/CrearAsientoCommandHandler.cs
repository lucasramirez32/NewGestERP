using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Contabilidad.Commands.CrearAsiento;

public class CrearAsientoCommandHandler : IRequestHandler<CrearAsientoCommand, AsientoDto>
{
    private readonly IAsientoRepository _asientosRepo;
    private readonly ICuentaContableRepository _cuentasRepo;
    private readonly IUnitOfWork _uow;

    public CrearAsientoCommandHandler(
        IAsientoRepository asientosRepo,
        ICuentaContableRepository cuentasRepo,
        IUnitOfWork uow)
    {
        _asientosRepo = asientosRepo;
        _cuentasRepo = cuentasRepo;
        _uow = uow;
    }

    public async Task<AsientoDto> Handle(CrearAsientoCommand request, CancellationToken ct)
    {
        var dto = request.Datos;

        // Validar que todas las cuentas existan y permitan imputación directa
        foreach (var p in dto.Partidas)
        {
            var cuenta = await _cuentasRepo.GetByIdAsync(p.IdCuenta, request.IdEmpresa, ct)
                ?? throw new DomainException($"Cuenta {p.IdCuenta} no encontrada.");

            if (!cuenta.ImputaDirectamente)
                throw new DomainException($"La cuenta '{cuenta.Codigo} - {cuenta.Descripcion}' es agrupadora y no acepta imputación directa.");
        }

        var numero = await _asientosRepo.ObtenerProximoNumeroAsync(request.IdEmpresa, ct);
        var asiento = Asiento.Crear(request.IdEmpresa, numero, dto.Fecha, dto.Descripcion, TipoAsiento.Manual);

        foreach (var p in dto.Partidas)
            asiento.AgregarPartida(p.IdCuenta, p.Debe, p.Haber, p.Concepto);

        asiento.Validar();

        await _asientosRepo.AddAsync(asiento, ct);
        await _uow.CommitAsync(ct);

        return await ToDto(asiento, request.IdEmpresa, ct);
    }

    private async Task<AsientoDto> ToDto(Asiento a, int idEmpresa, CancellationToken ct)
    {
        var partidas = new List<PartidaAsientoDto>();
        foreach (var p in a.Partidas)
        {
            var cuenta = await _cuentasRepo.GetByIdAsync(p.IdCuenta, idEmpresa, ct);
            partidas.Add(new PartidaAsientoDto(
                p.IdCuenta,
                cuenta?.Codigo ?? "",
                cuenta?.Descripcion ?? "",
                p.Debe, p.Haber, p.Concepto));
        }
        return new AsientoDto(a.IdAsiento, a.Numero, a.Fecha, a.Descripcion,
            a.TipoAsiento.ToString(), a.IdComprobanteOrigen,
            a.TotalDebe, a.TotalHaber, a.Anulado, partidas);
    }
}
