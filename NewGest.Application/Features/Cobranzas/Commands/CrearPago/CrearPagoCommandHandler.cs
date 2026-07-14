using MediatR;
using NewGest.Application.DTOs.Cobranzas;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Cobranzas.Commands.CrearPago;

public class CrearPagoCommandHandler : IRequestHandler<CrearPagoCommand, PagoDto>
{
    private readonly IPagoRepository             _pagosRepo;
    private readonly IComprobanteRepository      _comprobantesRepo;
    private readonly IAlicuotaRetencionRepository _alicuotasRepo;
    private readonly IClienteRepository          _clientesRepo;
    private readonly IUnitOfWork                 _uow;

    public CrearPagoCommandHandler(
        IPagoRepository pagosRepo,
        IComprobanteRepository comprobantesRepo,
        IAlicuotaRetencionRepository alicuotasRepo,
        IClienteRepository clientesRepo,
        IUnitOfWork uow)
    {
        _pagosRepo        = pagosRepo;
        _comprobantesRepo = comprobantesRepo;
        _alicuotasRepo    = alicuotasRepo;
        _clientesRepo     = clientesRepo;
        _uow              = uow;
    }

    public async Task<PagoDto> Handle(CrearPagoCommand request, CancellationToken ct)
    {
        var dto     = request.Datos;
        var cliente = await _clientesRepo.GetByIdAsync(request.IdEmpresa, dto.IdCliente, ct)
            ?? throw new DomainException($"Cliente {dto.IdCliente} no encontrado.");

        var numero = await _pagosRepo.ObtenerProximoNumeroAsync(request.IdEmpresa, ct);

        // Crear medios de pago
        var medios = dto.Medios.Select(m => MedioPago.Crear(
            m.Tipo, m.Monto, m.BancoEmisor, m.NumeroCheque,
            m.FechaVencimientoCheque, m.NumeroTransferencia)).ToList();

        var pago = Pago.Crear(request.IdEmpresa, dto.IdCliente, numero, dto.Fecha, medios, dto.Observaciones);

        // Imputar comprobantes — con validación de saldo disponible
        foreach (var imp in dto.Imputaciones)
        {
            var comp = await _comprobantesRepo.GetByIdAsync(imp.IdComprobante, request.IdEmpresa, ct)
                ?? throw new DomainException($"Comprobante {imp.IdComprobante} no encontrado.");

            if (comp.SaldoPendiente <= 0)
                throw new DomainException($"El comprobante {imp.IdComprobante} ya está completamente cobrado.");

            var montoAImputar = Math.Min(imp.Monto, comp.SaldoPendiente);
            pago.ImputarComprobante(imp.IdComprobante, montoAImputar);
            comp.ReducirSaldo(montoAImputar);
        }

        // Calcular retenciones usando alícuotas vigentes
        foreach (var ret in dto.Retenciones)
        {
            var pct = await _alicuotasRepo.ObtenerAlicuotaVigenteAsync(
                request.IdEmpresa, ret.Tipo, ret.Provincia, dto.Fecha, ct)
                ?? throw new DomainException(
                    $"No hay alícuota vigente para {ret.Tipo}/{ret.Provincia} al {dto.Fecha:dd/MM/yyyy}.");

            var nroFormulario = GenerarNumeroFormulario(request.IdEmpresa, ret.Tipo, numero);
            pago.AgregarRetencion(Retencion.Crear(
                request.IdEmpresa, ret.Tipo, ret.Provincia, pct, ret.BaseImponible, nroFormulario));
        }

        await _pagosRepo.AddAsync(pago, ct);
        await _uow.CommitAsync(ct);

        return ToDto(pago, cliente.RazonSocial);
    }

    internal static PagoDto ToDto(Pago p, string razonSocial) => new(
        p.IdPago, p.Numero, p.Fecha, p.IdCliente, razonSocial,
        p.TotalMedios, p.TotalImputado, p.SaldoAFavor, p.Anulado, p.Observaciones,
        p.Medios.Select(m => new MedioPagoDto(
            m.Tipo, m.Tipo.ToString(), m.Monto, m.BancoEmisor,
            m.NumeroCheque, m.FechaVencimientoCheque, m.NumeroTransferencia)).ToList(),
        p.Imputaciones.Select(i => new ImputacionDto(i.IdComprobante, "", i.Monto)).ToList(),
        p.Retenciones.Select(r => new RetencionDto(
            r.Tipo.ToString(), r.Provincia, r.Porcentaje,
            r.BaseImponible, r.MontoRetenido, r.NumeroFormulario)).ToList());

    private static string GenerarNumeroFormulario(int idEmpresa, TipoRetencion tipo, long nroPago) =>
        $"{tipo}-{idEmpresa:D4}-{nroPago:D8}";
}
