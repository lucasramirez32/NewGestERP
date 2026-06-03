namespace NewGest.Application.DTOs.Comprobantes;

public record ItemComprobanteDto(
    int IdArticulo,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    string Alicuota,
    decimal SubtotalNeto,
    decimal Iva,
    decimal Subtotal
);
