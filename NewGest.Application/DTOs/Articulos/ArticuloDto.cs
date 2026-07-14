namespace NewGest.Application.DTOs.Articulos;

public record ArticuloDto(
    int IdArticulo,
    int IdEmpresa,
    string Codigo,
    string Descripcion,
    int? IdGrupo,
    string? GrupoDescripcion,
    int IdUnidad,
    string UnidadDescripcion,
    string UnidadSimbolo,
    decimal PrecioLista,
    decimal PrecioCosto,
    decimal PorcentajeIva,
    string? Observaciones,
    bool Activo
);
