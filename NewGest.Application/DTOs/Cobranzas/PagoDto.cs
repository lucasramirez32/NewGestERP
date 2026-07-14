using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Cobranzas;

public record PagoDto(
    int IdPago,
    long Numero,
    DateOnly Fecha,
    int IdCliente,
    string RazonSocialCliente,
    decimal TotalMedios,
    decimal TotalImputado,
    decimal SaldoAFavor,
    bool Anulado,
    string? Observaciones,
    IReadOnlyList<MedioPagoDto> Medios,
    IReadOnlyList<ImputacionDto> Imputaciones,
    IReadOnlyList<RetencionDto> Retenciones
);

public record MedioPagoDto(
    TipoMedioPago Tipo,
    string TipoLabel,
    decimal Monto,
    string? BancoEmisor,
    string? NumeroCheque,
    DateOnly? FechaVencimientoCheque,
    string? NumeroTransferencia
);

public record ImputacionDto(int IdComprobante, string NumeroComprobante, decimal Monto);

public record RetencionDto(
    string Tipo, string? Provincia, decimal Porcentaje,
    decimal BaseImponible, decimal MontoRetenido, string NumeroFormulario
);

public record CrearPagoDto(
    DateOnly Fecha,
    int IdCliente,
    List<CrearMedioPagoDto> Medios,
    List<CrearImputacionDto> Imputaciones,
    List<CrearRetencionDto> Retenciones,
    string? Observaciones
);

public record CrearMedioPagoDto(
    TipoMedioPago Tipo, decimal Monto,
    string? BancoEmisor, string? NumeroCheque,
    DateOnly? FechaVencimientoCheque, string? NumeroTransferencia
);

public record CrearImputacionDto(int IdComprobante, decimal Monto);

public record CrearRetencionDto(TipoRetencion Tipo, string? Provincia, decimal BaseImponible);

public record ComprobantePendienteDto(
    int IdComprobante, string TipoComprobante, int PuntoVenta, long Numero,
    DateOnly Fecha, decimal Total, decimal SaldoPendiente
);
