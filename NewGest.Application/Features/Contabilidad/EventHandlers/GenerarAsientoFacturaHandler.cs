using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Contabilidad.EventHandlers;

/// <summary>
/// Genera el asiento contable automático cuando se asigna un CAE a un comprobante.
/// - Facturas (A/B/C/M): Debe CxC, Haber Ventas + Haber IVA
/// - Notas de Crédito (A/B/C): asiento inverso — Debe Ventas + Debe IVA, Haber CxC
/// - Notas de Débito (A/B/C): igual a factura (aumentan deuda del cliente)
/// Fix Bug#3: usa Math.Round de cada parcial para evitar drift de centavo
/// Fix Bug#4: persiste el asiento directamente sin llamar a _uow.CommitAsync
///            para no abrir una segunda transacción dentro del domain event handler
/// </summary>
public class GenerarAsientoFacturaHandler : INotificationHandler<CaeAsignadoNotification>
{
    private readonly IComprobanteRepository _comprobantesRepo;
    private readonly IAsientoRepository _asientosRepo;
    private readonly IParametroRepository _parametrosRepo;

    public GenerarAsientoFacturaHandler(
        IComprobanteRepository comprobantesRepo,
        IAsientoRepository asientosRepo,
        IParametroRepository parametrosRepo)
    {
        _comprobantesRepo = comprobantesRepo;
        _asientosRepo = asientosRepo;
        _parametrosRepo = parametrosRepo;
    }

    public async Task Handle(CaeAsignadoNotification notification, CancellationToken ct)
    {
        var comp = await _comprobantesRepo.GetByIdAsync(notification.IdComprobante, notification.IdEmpresa, ct);
        if (comp is null) return;

        var cuentaCxC    = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_CXC",       ct);
        var cuentaVentas = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_VENTAS",     ct);
        var cuentaIva    = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_IVA_VENTAS", ct);

        var numero = await _asientosRepo.ObtenerProximoNumeroAsync(comp.IdEmpresa, ct);
        var desc   = $"{comp.Tipo} {comp.PuntoVenta:D4}-{comp.Numero:D8} — {comp.RazonSocialCliente}";

        var asiento = Asiento.Crear(comp.IdEmpresa, numero, comp.Fecha, desc,
            TipoAsiento.AutoFactura, comp.IdComprobante);

        // Fix Bug#3: redondear cada componente al centavo antes de armar partidas
        var totalNeto = Math.Round(comp.TotalNeto, 2);
        var totalIva  = Math.Round(comp.TotalIva, 2);
        var total     = totalNeto + totalIva;   // recalculado desde las partes redondeadas

        if (EsNotaCredito(comp.Tipo))
        {
            // NC: reversa la factura original — disminuye CxC y reversa Ventas + IVA
            asiento.AgregarPartida(cuentaVentas, debe: totalNeto, haber: 0, "Reversa Ventas NC");
            if (totalIva > 0)
                asiento.AgregarPartida(cuentaIva, debe: totalIva, haber: 0, "Reversa IVA NC");
            asiento.AgregarPartida(cuentaCxC, debe: 0, haber: total, "Cuentas a cobrar NC");
        }
        else
        {
            // Facturas y Notas de Débito: aumentan la deuda del cliente
            asiento.AgregarPartida(cuentaCxC, debe: total, haber: 0, "Cuentas a cobrar");
            asiento.AgregarPartida(cuentaVentas, debe: 0, haber: totalNeto, "Ventas");
            if (totalIva > 0)
                asiento.AgregarPartida(cuentaIva, debe: 0, haber: totalIva, "IVA Ventas");
        }

        asiento.Validar();

        // Fix Bug#4: solo AddAsync — el SaveChanges lo hace el UnitOfWork del request original
        // que llamó a CommitAsync y disparó este domain event handler.
        // No llamar a _uow.CommitAsync aquí para evitar una transacción separada.
        await _asientosRepo.AddAsync(asiento, ct);
    }

    private static bool EsNotaCredito(TipoComprobante tipo) => tipo is
        TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC;

    private async Task<int> ObtenerIdCuenta(int idEmpresa, string clave, CancellationToken ct)
    {
        var param = await _parametrosRepo.ObtenerPorClaveAsync(clave, idEmpresa, ct)
            ?? throw new DomainException($"Parámetro contable '{clave}' no configurado para empresa {idEmpresa}.");

        if (!int.TryParse(param.Valor, out var idCuenta))
            throw new DomainException($"Parámetro '{clave}' tiene valor inválido: '{param.Valor}'.");

        return idCuenta;
    }
}

/// <summary>
/// Adaptador Domain→MediatR para CaeAsignadoEvent.
/// Domain no depende de MediatR; este record vive en Application.
/// </summary>
public record CaeAsignadoNotification(int IdComprobante, int IdEmpresa, string CodigoCae)
    : INotification;
