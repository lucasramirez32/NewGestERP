namespace NewGest.Application.DTOs.Articulos;

public record GrupoArticuloDto(
    int IdGrupo,
    string Descripcion,
    int? IdGrupoPadre,
    IReadOnlyList<GrupoArticuloDto> Subgrupos
);
