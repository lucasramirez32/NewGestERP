using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Pedidos;

public record PedidoDto(
    int IdPedido,
    int IdEmpresa,
    int IdCliente,
    string RazonSocialCliente,
    int? IdVendedor,
    string? NombreVendedor,
    EstadoPedido Estado,
    string EstadoDescripcion,
    DateTime FechaPedido,
    DateTime? FechaEntregaEstimada,
    string? Observaciones,
    List<ItemPedidoDto> Items);

public record PedidoListItemDto(
    int IdPedido,
    int IdCliente,
    string RazonSocialCliente,
    int? IdVendedor,
    string? NombreVendedor,
    EstadoPedido Estado,
    string EstadoDescripcion,
    DateTime FechaPedido,
    DateTime? FechaEntregaEstimada,
    int CantidadItems);
