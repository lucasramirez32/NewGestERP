namespace NewGest.Application.DTOs.Reportes;

// ─── Comprobante PDF ──────────────────────────────────────────────────────────
public record ComprobanteReportDto(
    // Empresa emisora
    string NombreEmpresa,
    string RazonSocialEmpresa,
    string CuitEmpresa,
    string CondicionIvaEmpresa,
    string DomicilioEmpresa,
    // Comprobante
    string TipoLabel,
    int PuntoVenta,
    long Numero,
    DateOnly Fecha,
    // Receptor
    string RazonSocialCliente,
    string? CuitCliente,
    string CondicionIvaCliente,
    string? DomicilioCliente,
    // Items
    IReadOnlyList<ItemReportDto> Items,
    // Totales por alícuota
    IReadOnlyList<AlicuotaReportDto> Alicuotas,
    decimal TotalNeto,
    decimal TotalIva,
    decimal Total,
    // CAE + QR
    string? CodigoCae,
    DateOnly? VencimientoCae,
    string? QrUrl,
    byte[]? QrPngBytes    // Fix BUG-01: PNG precalculado por el endpoint via IQrFiscalService
);

public record ItemReportDto(
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    string Alicuota,
    decimal Subtotal
);

public record AlicuotaReportDto(string Porcentaje, decimal Base, decimal Iva);

// ─── Libro IVA PDF ────────────────────────────────────────────────────────────
public record LibroIvaReportDto(
    string NombreEmpresa,
    string CuitEmpresa,
    int Anio,
    int Mes,
    string TipoLibro,
    IReadOnlyList<LineaLibroIvaReportDto> Lineas,
    // Fix BUG-03: totales desglosados por alícuota (RG AFIP 3685/2014)
    decimal TotalNeto21,
    decimal TotalIva21,
    decimal TotalNeto105,
    decimal TotalIva105,
    decimal TotalExento,
    decimal TotalGeneral
);

// Fix BUG-03: desglose de alícuotas por línea
public record LineaLibroIvaReportDto(
    DateOnly Fecha,
    string Comprobante,
    string RazonSocial,
    string? Cuit,
    decimal Neto21,
    decimal Iva21,
    decimal Neto105,
    decimal Iva105,
    decimal Exento,
    decimal Total
);

// ─── Estado de cuenta cliente ─────────────────────────────────────────────────
public record EstadoCuentaReportDto(
    string NombreEmpresa,
    string CuitEmpresa,
    string RazonSocialCliente,
    string? CuitCliente,
    DateOnly FechaDesde,
    DateOnly FechaHasta,
    IReadOnlyList<MovimientoCuentaDto> Movimientos,
    decimal SaldoAnterior,
    decimal SaldoFinal
);

public record MovimientoCuentaDto(
    DateOnly Fecha,
    string Comprobante,
    string Descripcion,
    decimal Debe,
    decimal Haber,
    decimal SaldoAcumulado
);

// ─── Exports Excel ────────────────────────────────────────────────────────────
public record ClienteExportDto(
    string Codigo, string RazonSocial, string? Cuit,
    string CondicionIva, string? Localidad, string? Telefono, string? Email);

public record ArticuloExportDto(
    string Codigo, string Descripcion, string Grupo,
    string Unidad, decimal PrecioVenta, bool Activo);

public record MovimientoExportDto(
    DateOnly Fecha, string Articulo, string Deposito,
    string Tipo, decimal Cantidad, decimal CostoUnitario);
