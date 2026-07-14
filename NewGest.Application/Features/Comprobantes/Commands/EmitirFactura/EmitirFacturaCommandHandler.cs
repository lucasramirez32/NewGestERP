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
    private readonly IEmpresaRepository _empresasRepo;
    private readonly IStockService _stockService;
    private readonly IAfipService _afipService;
    private readonly IUnitOfWork _uow;

    public EmitirFacturaCommandHandler(
        IComprobanteRepository comprobantesRepo,
        IClienteRepository clientesRepo,
        IEmpresaRepository empresasRepo,
        IStockService stockService,
        IAfipService afipService,
        IUnitOfWork uow)
    {
        _comprobantesRepo = comprobantesRepo;
        _clientesRepo = clientesRepo;
        _empresasRepo = empresasRepo;
        _stockService = stockService;
        _afipService = afipService;
        _uow = uow;
    }

    public async Task<FacturaEmitidaDto> Handle(EmitirFacturaCommand request, CancellationToken ct)
    {
        var empresa = await _empresasRepo.ObtenerPorIdAsync(request.IdEmpresa, ct)
            ?? throw new DomainException($"Empresa {request.IdEmpresa} no encontrada.");

        var cuitEmisor = empresa.Cuit
            ?? throw new DomainException("La empresa no tiene CUIT configurado.");

        var cliente = await _clientesRepo.GetByIdAsync(request.IdEmpresa, request.IdCliente, ct)
            ?? throw new DomainException($"Cliente {request.IdCliente} no encontrado.");

        // El número se reserva de forma atómica antes de llamar a AFIP
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

        if (EsElectronica(request.Tipo))
        {
            var afipRequest = BuildAfipRequest(cuitEmisor, comprobante, numero, items);
            var caeResponse = await _afipService.SolicitarCaeAsync(afipRequest, ct);
            comprobante.AsignarCae(caeResponse.CodigoCae, caeResponse.FechaVencimiento);
        }

        // Fix 3: AddAsync y DescontarStock antes del commit único para garantizar atomicidad.
        // Si CommitAsync falla, ni el comprobante ni el ajuste de stock quedan persistidos.
        await _comprobantesRepo.AddAsync(comprobante, ct);

        foreach (var item in items)
            await _stockService.DescontarStockAsync(request.IdEmpresa, item.IdArticulo, item.Cantidad, ct);

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

    // Fix 4: FacturaM (código 51 AFIP) es electrónica
    public static bool EsElectronica(TipoComprobante tipo) => tipo is
        TipoComprobante.FacturaA   or TipoComprobante.FacturaB   or TipoComprobante.FacturaC   or
        TipoComprobante.FacturaM   or
        TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC or
        TipoComprobante.NotaDebitoA  or TipoComprobante.NotaDebitoB  or TipoComprobante.NotaDebitoC;

    // Fix 2: mapear TODAS las alícuotas, no solo 21% y 10.5%
    public static ComprobanteAfip BuildAfipRequest(
        string cuitEmisor, Comprobante comp, long numero, List<ItemComprobante> items)
    {
        var alicuotas = items
            .Where(i => i.Alicuota != AlicuotaIva.Exento && i.Alicuota != AlicuotaIva.Porcentaje0)
            .GroupBy(i => i.Alicuota)
            .Select(g => new AlicuotaAfipDetalle(
                (int)g.Key,
                g.Sum(i => i.SubtotalNeto),
                g.Sum(i => i.Iva)))
            .ToList();

        var exento = items
            .Where(i => i.Alicuota is AlicuotaIva.Exento or AlicuotaIva.Porcentaje0)
            .Sum(i => i.SubtotalNeto);

        return new ComprobanteAfip(
            cuitEmisor,
            comp.Tipo,
            comp.PuntoVenta,
            numero,
            numero,
            comp.Fecha,
            comp.CuitCliente,
            exento,
            comp.Total,
            alicuotas);
    }
}
