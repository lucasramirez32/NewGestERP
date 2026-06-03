namespace NewGest.Domain.Entities.Neg;

public class Viaje
{
    public int IdViaje { get; set; }
    public int IdEmpresa { get; set; }
    public string Descripcion { get; set; } = default!;
    public DateTime FechaViaje { get; set; }
    public string? Destino { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
}
