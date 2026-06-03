using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Interfaces;
using NewGest.Application.Services;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

public class EmitirFacturaCommandHandler : IRequestHandler<EmitirFacturaCommand, FacturaEmitidaDto>
{
    private readonly IComprobanteRepository _comprobantesRepo;
    private readonly IClienteRepository _clientesRepo;
    private readonly IStockService _stockService;
    private readonly IAfipService _afipService;
    private readonly IUnitOfWork _uow;

    public EmitirFacturaCommandHandler(
        IComprobanteRepository comprobantesRepo,
        IClienteRepository clientesRepo,
        IStockService stockService,
        IAfipService afipService,
        IUnitOfWork uow)
    {
        _comprobantesRepo = comprobantesRepo;
        _clientesRepo = clientesRepo;
        _stockService = stockService;
        _afipService = afipService;
        _uow = uow;
    }

    public async Task<FacturaEmitidaDto> Handle(EmitirFacturaCommand request, CancellationToken ct)
    {
        var cliente = await _clientesRepo.GetByIdAsync(request.IdCliente, request.IdEmpresa, ct)
            ?? throw new DomainException($"Cliente {request.IdCliente} no encontrado.");

        var numero = await _comprobantesRepo.ObtenerProximoNumeroAsync(
            request.IdEmpresa, request.PuntoVenta, request.Tipo, ct);

        var items = request.Items
            .Select(i => ItemComprobante.Crear(i.IdArticulo, i.Descripcion, i.Cantidad, i.PrecioUnitario, i.Alicuota))
            .ToList();

        var comprobante = Comprobante.Crear(
            request.IdEmpresa,
            request.Tipo,
            request.PuntoVenta,
            numero,
            request.Fecha,
            request.IdCliente,
            cliente.RazonSocial,
            cliente.CUIT,
            cliente.CondicionIva,
            items,
            request.IdPedidoOrigen);

        // Solo facturas electrónicas (A, B, C) solicitan CAE
        if (EsElectronica(request.Tipo))
        {
            var afipRequest = BuildAfipRequest(request.CuitEmisor, comprobante, numero, items);
            var caeResponse = await _afipService.SolicitarCaeAsync(afipRequest, ct);
            comprobante.AsignarCae(caeResponse.CodigoCae, caeResponse.FechaVencimiento);
        }

        // Descontar stock en artículos de inventario
        foreach (var item in items)
        {
            await _stockService.DescontarStockAsync(request.IdEmpresa, item.IdArticulo, item.Cantidad, ct);
        }

        await _comprobantesRepo.AddAsync(comprobante, ct);
        await _uow.CommitAsync(ct);

        return new FacturaEmitidaDto(
            comprobante.IdComprobante,
            comprobante.Tipo.ToString(),
            comprobante.PuntoVenta,
            comprobante.Numero,
            comprobante.Cae?.Codigo,
            comprobante.Cae?.FechaVencimiento,
            comprobante.Total);
    }

    private static bool EsElectronica(TipoComprobante tipo) => tipo is
        TipoComprobante.FacturaA or TipoComprobante.FacturaB or TipoComprobante.FacturaC or
        TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC or
        TipoComprobante.NotaDebitoA or TipoComprobante.NotaDebitoB or TipoComprobante.NotaDebitoC;

    private static ComprobanteAfip BuildAfipRequest(
        string cuitEmisor, Comprobante comp, long numero, List<ItemComprobante> items)
    {
        var neto21 = items.Where(i => i.Alicuota == AlicuotaIva.Porcentaje21).Sum(i => i.SubtotalNeto);
        var iva21 = items.Where(i => i.Alicuota == AlicuotaIva.Porcentaje21).Sum(i => i.Iva);
        var neto105 = items.Where(i => i.Alicuota == AlicuotaIva.Porcentaje10_5).Sum(i => i.SubtotalNeto);
        var iva105 = items.Where(i => i.Alicuota == AlicuotaIva.Porcentaje10_5).Sum(i => i.Iva);
        var exento = items.Where(i => i.Alicuota == AlicuotaIva.Exento).Sum(i => i.SubtotalNeto);

        return new ComprobanteAfip(
            cuitEmisor,
            comp.Tipo,
            comp.PuntoVenta,
            numero,
            numero,
            comp.Fecha,
            comp.CuitCliente,
            neto21, iva21,
            neto105, iva105,
            exento,
            comp.Total);
    }
}
