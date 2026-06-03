using MediatR;

namespace NewGest.Application.Features.Stock.Queries.GetStockValorizado;

public record StockValorizadoItemDto(
    int IdArticulo,
    string CodigoArticulo,
    string DescripcionArticulo,
    int IdDeposito,
    string NombreDeposito,
    decimal Cantidad,
    decimal CostoPromedio,
    decimal ValorTotal);

public record GetStockValorizadoQuery(int IdEmpresa) : IRequest<IReadOnlyList<StockValorizadoItemDto>>;
