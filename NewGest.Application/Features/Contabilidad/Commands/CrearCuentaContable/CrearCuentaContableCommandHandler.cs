using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.Features.Contabilidad.Queries.GetPlanCuentas;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Contabilidad.Commands.CrearCuentaContable;

public class CrearCuentaContableCommandHandler : IRequestHandler<CrearCuentaContableCommand, CuentaContableDto>
{
    private readonly ICuentaContableRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearCuentaContableCommandHandler(ICuentaContableRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<CuentaContableDto> Handle(CrearCuentaContableCommand request, CancellationToken ct)
    {
        var dto = request.Datos;

        var existente = await _repo.GetByCodigoAsync(request.IdEmpresa, dto.Codigo, ct);
        if (existente is not null)
            throw new DomainException($"Ya existe una cuenta con el código '{dto.Codigo}'.");

        var cuenta = CuentaContable.Crear(
            request.IdEmpresa, dto.Codigo, dto.Descripcion, dto.IdCuentaPadre,
            (NaturalezaCuenta)dto.Naturaleza, (TipoCuenta)dto.Tipo, dto.ImputaDirectamente);

        await _repo.AddAsync(cuenta, ct);
        await _uow.CommitAsync(ct);

        return GetPlanCuentasQueryHandler.ToDto(cuenta, new Dictionary<int, CuentaContable>());
    }
}
