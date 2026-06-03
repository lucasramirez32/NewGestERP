namespace NewGest.Application.DTOs.Pedidos;

public record CrearPedidoDto(
    int IdCliente,
    int? IdVendedor,
    DateTime? FechaEntregaEstimada,
    string? Observaciones,
    List<ItemPedidoInputDto> Items);
