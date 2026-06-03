namespace NewGest.Application.DTOs.Personal;

public record MutualDto(
    int IdMutual,
    string Codigo,
    string Descripcion,
    bool Activo);

public record CrearMutualDto(string Codigo, string Descripcion);

public record ActualizarMutualDto(string Descripcion);
