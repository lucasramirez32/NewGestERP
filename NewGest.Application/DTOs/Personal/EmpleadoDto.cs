using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Personal;

public record EmpleadoDto(
    int IdEmpleado,
    int IdEmpresa,
    string Legajo,
    string ApellidoNombre,
    string? CUIL,
    RolEmpleado Rol,
    string RolDescripcion,
    bool EsVendedor,
    decimal? ComisionPorcentaje,
    bool Activo);

public record EmpleadoListItemDto(
    int IdEmpleado,
    string Legajo,
    string ApellidoNombre,
    string? CUIL,
    RolEmpleado Rol,
    string RolDescripcion,
    bool EsVendedor,
    decimal? ComisionPorcentaje,
    bool Activo);
