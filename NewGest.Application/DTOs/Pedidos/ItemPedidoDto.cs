namespace NewGest.Application.DTOs.Pedidos;

public record ItemPedidoInputDto(int IdArticulo, decimal Cantidad, decimal PrecioUnitario);

public record ItemPedidoDto(
    int IdItemPedido,
    int IdArticulo,
    string DescripcionArticulo,
    decimal CantidadPedida,
    decimal CantidadEntregada,
    decimal CantidadPendiente,
    decimal PrecioUnitario);
