using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Comprobantes;

public record EmitirFacturaDto(
    TipoComprobante Tipo,
    int PuntoVenta,
    DateOnly Fecha,
    int IdCliente,
    int? IdPedidoOrigen,
    List<ItemFacturaDto> Items
);

public record ItemFacturaDto(
    int IdArticulo,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    AlicuotaIva Alicuota
);
