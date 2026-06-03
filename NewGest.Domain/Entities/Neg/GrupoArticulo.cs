namespace NewGest.Domain.Entities.Neg;

public class GrupoArticulo
{
    public int IdGrupo { get; set; }
    public int IdEmpresa { get; set; }
    public string Descripcion { get; set; } = default!;
    public int? IdGrupoPadre { get; set; }
    public GrupoArticulo? GrupoPadre { get; set; }
    public ICollection<GrupoArticulo> Subgrupos { get; set; } = [];
    public ICollection<Articulo> Articulos { get; set; } = [];
}
