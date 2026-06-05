using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;
using NewGest.Domain.Events;

namespace NewGest.Application.Features.Contabilidad.EventHandlers;

/// <summary>
/// Genera el asiento contable automático cuando se asigna un CAE a un comprobante.
/// Las cuentas (CxC, Ventas, IVA Ventas) se obtienen de cfg.Parametros de la empresa.
/// </summary>
public class GenerarAsientoFacturaHandler : INotificationHandler<CaeAsignadoNotification>
{
    private readonly IComprobanteRepository _comprobantesRepo;
    private readonly IAsientoRepository _asientosRepo;
    private readonly ICuentaContableRepository _cuentasRepo;
    private readonly IParametroRepository _parametrosRepo;
    private readonly IUnitOfWork _uow;

    public GenerarAsientoFacturaHandler(
        IComprobanteRepository comprobantesRepo,
        IAsientoRepository asientosRepo,
        ICuentaContableRepository cuentasRepo,
        IParametroRepository parametrosRepo,
        IUnitOfWork uow)
    {
        _comprobantesRepo = comprobantesRepo;
        _asientosRepo = asientosRepo;
        _cuentasRepo = cuentasRepo;
        _parametrosRepo = parametrosRepo;
        _uow = uow;
    }

    public async Task Handle(CaeAsignadoNotification notification, CancellationToken ct)
    {
        var comp = await _comprobantesRepo.GetByIdAsync(notification.IdComprobante, notification.IdEmpresa, ct);
        if (comp is null) return;

        // Obtener cuentas contables configuradas en parámetros
        var cuentaCxC     = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_CXC",      ct);
        var cuentaVentas  = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_VENTAS",    ct);
        var cuentaIva     = await ObtenerIdCuenta(comp.IdEmpresa, "CUENTA_IVA_VENTAS", ct);

        var numero = await _asientosRepo.ObtenerProximoNumeroAsync(comp.IdEmpresa, ct);
        var descripcion = $"{comp.Tipo} {comp.PuntoVenta:D4}-{comp.Numero:D8} — {comp.RazonSocialCliente}";

        var asiento = Asiento.Crear(
            comp.IdEmpresa, numero, comp.Fecha, descripcion,
            TipoAsiento.AutoFactura, comp.IdComprobante);

        // Débito: Cuentas a cobrar por el total del comprobante
        asiento.AgregarPartida(cuentaCxC, debe: comp.Total, haber: 0, "Cuentas a cobrar");

        // Crédito: Ventas por el neto
        asiento.AgregarPartida(cuentaVentas, debe: 0, haber: comp.TotalNeto, "Ventas");

        // Crédito: IVA Ventas (si hay)
        if (comp.TotalIva > 0)
            asiento.AgregarPartida(cuentaIva, debe: 0, haber: comp.TotalIva, "IVA Ventas");

        asiento.Validar();
        await _asientosRepo.AddAsync(asiento, ct);
        await _uow.CommitAsync(ct);
    }

    private async Task<int> ObtenerIdCuenta(int idEmpresa, string clave, CancellationToken ct)
    {
        var param = await _parametrosRepo.ObtenerPorClaveAsync(clave, idEmpresa, ct)
            ?? throw new DomainException($"Parámetro contable '{clave}' no configurado para empresa {idEmpresa}.");

        if (!int.TryParse(param.Valor, out var idCuenta))
            throw new DomainException($"Parámetro '{clave}' tiene un valor inválido: '{param.Valor}'.");

        return idCuenta;
    }
}

/// <summary>
/// Wrapper de CaeAsignadoEvent para que MediatR pueda despacharlo como INotification.
/// Domain no depende de MediatR; este adaptador vive en Application.
/// </summary>
public record CaeAsignadoNotification(int IdComprobante, int IdEmpresa, string CodigoCae)
    : INotification;
