namespace NewGest.Application.DTOs.Stock;

public record DepositoDto(int IdDeposito, string Codigo, string Descripcion, bool Activo);

public record CrearDepositoDto(string Codigo, string Descripcion);
