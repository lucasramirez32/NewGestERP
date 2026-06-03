namespace NewGest.Application.DTOs.Pedidos;

public record ItemRemitoDto(
    int IdItemRemito,
    int IdArticulo,
    string DescripcionArticulo,
    decimal Cantidad,
    decimal PrecioUnitario);

public record RemitoDto(
    int IdRemito,
    int IdEmpresa,
    int IdPedido,
    int IdCliente,
    string RazonSocialCliente,
    DateTime FechaRemito,
    string? Observaciones,
    List<ItemRemitoDto> Items);
