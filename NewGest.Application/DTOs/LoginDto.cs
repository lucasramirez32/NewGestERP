namespace NewGest.Application.DTOs;

public record LoginDto(int IdEmpresa, string NombreUsuario, string Password);

public record LoginResultDto(
    string Token,
    int IdUsuario,
    string Nombre,
    int IdEmpresa,
    bool DebeResetearPassword);

public record EmpresaDto(int IdEmpresa, string Nombre, string? Rut);

public record CambiarPasswordDto(string PasswordActual, string PasswordNueva);

public record ParametroDto(
    int IdParametro,
    int? IdEmpresa,
    string Clave,
    string Valor,
    string Descripcion);

public record ActualizarParametroDto(string Valor);
