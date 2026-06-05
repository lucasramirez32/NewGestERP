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
    string TipoLabel,          // "FACTURA B", "NOTA DE CRÉDITO A", etc.
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
    // CAE
    string? CodigoCae,
    DateOnly? VencimientoCae,
    string? QrUrl
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
    decimal TotalNeto,
    decimal TotalIva,
    decimal TotalGeneral
);

public record LineaLibroIvaReportDto(
    DateOnly Fecha,
    string Comprobante,
    string RazonSocial,
    string? Cuit,
    decimal Neto,
    decimal Iva,
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
