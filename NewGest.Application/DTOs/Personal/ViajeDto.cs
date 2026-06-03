namespace NewGest.Application.DTOs.Personal;

public record ViajeDto(
    int IdViaje,
    string Descripcion,
    DateTime FechaViaje,
    string? Destino,
    string? Observaciones,
    bool Activo);

public record CrearViajeDto(
    string Descripcion,
    DateTime FechaViaje,
    string? Destino,
    string? Observaciones);

public record ActualizarViajeDto(
    string Descripcion,
    DateTime FechaViaje,
    string? Destino,
    string? Observaciones);
