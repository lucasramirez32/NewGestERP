namespace NewGest.Application.DTOs.Articulos;

public record ArticuloListItemDto(
    int IdArticulo,
    string Codigo,
    string Descripcion,
    string GrupoDescripcion,
    string UnidadSimbolo,
    decimal PrecioLista,
    decimal PorcentajeIva,
    bool Activo
);
