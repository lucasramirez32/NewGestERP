using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Com;

namespace NewGest.Application.Features.Comprobantes.Queries.GetComprobantes;

public class GetComprobantesQueryHandler : IRequestHandler<GetComprobantesQuery, PagedResult<ComprobanteDto>>
{
    private readonly IComprobanteRepository _repo;

    public GetComprobantesQueryHandler(IComprobanteRepository repo) => _repo = repo;

    public async Task<PagedResult<ComprobanteDto>> Handle(GetComprobantesQuery request, CancellationToken ct)
    {
        var (items, total) = await _repo.GetPagedAsync(request.IdEmpresa, request.Pagina, request.Tamano, ct);
        return new PagedResult<ComprobanteDto>(items.Select(ToDto).ToList(), total, request.Pagina, request.Tamano);
    }

    internal static ComprobanteDto ToDto(Comprobante c) => new(
        c.IdComprobante,
        c.Tipo.ToString(),
        c.PuntoVenta,
        c.Numero,
        c.Fecha,
        c.RazonSocialCliente,
        c.CuitCliente,
        c.CondicionIvaReceptor.ToString(),
        c.TotalNeto,
        c.TotalIva,
        c.Total,
        c.Cae?.Codigo,
        c.Cae?.FechaVencimiento,
        c.EsElectronica,
        c.Anulado,
        c.Items.Select(i => new ItemComprobanteDto(
            i.IdArticulo, i.Descripcion, i.Cantidad, i.PrecioUnitario,
            i.Alicuota.ToString(), i.SubtotalNeto, i.Iva, i.Subtotal)).ToList());
}
