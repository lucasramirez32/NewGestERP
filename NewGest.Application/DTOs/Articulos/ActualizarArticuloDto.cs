namespace NewGest.Application.DTOs.Articulos;

public record ActualizarArticuloDto(
    string Descripcion,
    int? IdGrupo,
    int IdUnidad,
    decimal PrecioLista,
    decimal PrecioCosto,
    decimal PorcentajeIva,
    string? Observaciones = null,
    string? NombreGrupo = null
);
