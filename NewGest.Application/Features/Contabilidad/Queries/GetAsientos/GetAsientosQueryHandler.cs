using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Cnt;

namespace NewGest.Application.Features.Contabilidad.Queries.GetAsientos;

public class GetAsientosQueryHandler : IRequestHandler<GetAsientosQuery, PagedResult<AsientoDto>>
{
    private readonly IAsientoRepository _asientosRepo;
    private readonly ICuentaContableRepository _cuentasRepo;

    public GetAsientosQueryHandler(IAsientoRepository asientosRepo, ICuentaContableRepository cuentasRepo)
    {
        _asientosRepo = asientosRepo;
        _cuentasRepo = cuentasRepo;
    }

    public async Task<PagedResult<AsientoDto>> Handle(GetAsientosQuery request, CancellationToken ct)
    {
        var (items, total) = await _asientosRepo.GetPagedAsync(request.IdEmpresa, request.Pagina, request.Tamano, ct);
        var cuentas = (await _cuentasRepo.GetAllAsync(request.IdEmpresa, ct)).ToDictionary(c => c.IdCuenta);

        var dtos = items.Select(a => ToDto(a, cuentas)).ToList();
        return new PagedResult<AsientoDto>(dtos, total, request.Pagina, request.Tamano);
    }

    internal static AsientoDto ToDto(Asiento a, Dictionary<int, CuentaContable> cuentas)
    {
        var partidas = a.Partidas.Select(p =>
        {
            cuentas.TryGetValue(p.IdCuenta, out var c);
            return new PartidaAsientoDto(p.IdCuenta, c?.Codigo ?? "", c?.Descripcion ?? "", p.Debe, p.Haber, p.Concepto);
        }).ToList();

        return new AsientoDto(a.IdAsiento, a.Numero, a.Fecha, a.Descripcion,
            a.TipoAsiento.ToString(), a.IdComprobanteOrigen,
            a.TotalDebe, a.TotalHaber, a.Anulado, partidas);
    }
}
