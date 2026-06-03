namespace NewGest.Application.DTOs.Pedidos;

public record DespacharItemDto(int IdArticulo, decimal Cantidad);

public record GenerarRemitoDto(int IdDeposito, List<DespacharItemDto> Items);
