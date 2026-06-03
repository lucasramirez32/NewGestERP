using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Personal;

public record CrearEmpleadoDto(
    string Legajo,
    string ApellidoNombre,
    string? CUIL,
    RolEmpleado Rol,
    decimal? ComisionPorcentaje);

public record ActualizarEmpleadoDto(
    string ApellidoNombre,
    string? CUIL,
    RolEmpleado Rol,
    decimal? ComisionPorcentaje);
