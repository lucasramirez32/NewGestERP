namespace NewGest.Domain.Entities.Config;

public class Parametro
{
    public int IdParametro { get; set; }

    /// <summary>null indica parámetro global (no asociado a empresa particular).</summary>
    public int? IdEmpresa { get; set; }

    public string Clave { get; set; } = default!;
    public string Valor { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
}
