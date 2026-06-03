namespace NewGest.Domain.Entities.Neg;

public class Mutual
{
    public int IdMutual { get; set; }
    public int IdEmpresa { get; set; }
    public string Codigo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public bool Activo { get; set; } = true;
}
