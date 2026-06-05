using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Cnt;

namespace NewGest.Application.Features.Contabilidad.Queries.GetPlanCuentas;

public class GetPlanCuentasQueryHandler : IRequestHandler<GetPlanCuentasQuery, IReadOnlyList<CuentaContableDto>>
{
    private readonly ICuentaContableRepository _repo;

    public GetPlanCuentasQueryHandler(ICuentaContableRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CuentaContableDto>> Handle(GetPlanCuentasQuery request, CancellationToken ct)
    {
        var cuentas = await _repo.GetAllAsync(request.IdEmpresa, ct);
        var mapa = cuentas.ToDictionary(c => c.IdCuenta);

        return cuentas
            .OrderBy(c => c.Codigo)
            .Select(c => ToDto(c, mapa))
            .ToList();
    }

    internal static CuentaContableDto ToDto(CuentaContable c, Dictionary<int, CuentaContable> mapa)
    {
        var codigoPadre = c.IdCuentaPadre.HasValue && mapa.TryGetValue(c.IdCuentaPadre.Value, out var padre)
            ? padre.Codigo
            : "";

        return new CuentaContableDto(
            c.IdCuenta, c.Codigo, c.Descripcion, c.IdCuentaPadre, codigoPadre,
            c.Naturaleza.ToString(), c.Tipo.ToString(),
            c.ImputaDirectamente, c.Activa);
    }
}
