using MediatR;
using NewGest.Application.DTOs.Cobranzas;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Cobranzas.Queries.GetPendientes;

public class GetPendientesQueryHandler : IRequestHandler<GetPendientesQuery, IReadOnlyList<ComprobantePendienteDto>>
{
    private readonly IComprobanteRepository _repo;

    public GetPendientesQueryHandler(IComprobanteRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ComprobantePendienteDto>> Handle(GetPendientesQuery request, CancellationToken ct)
    {
        var (comprobantes, _) = await _repo.GetPagedAsync(request.IdEmpresa, 1, 1000, ct);

        return comprobantes
            .Where(c => !c.Anulado && c.IdCliente == request.IdCliente && c.SaldoPendiente > 0)
            .OrderBy(c => c.Fecha)
            .Select(c => new ComprobantePendienteDto(
                c.IdComprobante,
                c.Tipo.ToString(),
                c.PuntoVenta,
                c.Numero,
                c.Fecha,
                c.Total,
                c.SaldoPendiente))
            .ToList();
    }
}
