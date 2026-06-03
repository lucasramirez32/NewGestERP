namespace NewGest.Application.DTOs.Stock;

public record ExistenciaDepositoDto(
    int IdExistencia,
    int IdArticulo,
    string CodigoArticulo,
    string DescripcionArticulo,
    int IdDeposito,
    string NombreDeposito,
    decimal Cantidad,
    decimal CostoPromedio,
    decimal StockMinimo,
    string NivelBadge);   // "verde" | "amarillo" | "rojo"
