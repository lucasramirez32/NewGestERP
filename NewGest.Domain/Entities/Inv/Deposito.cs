namespace NewGest.Domain.Entities.Inv;

public class Deposito
{
    public int IdDeposito { get; set; }
    public int IdEmpresa { get; set; }
    public string Codigo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public bool Activo { get; set; } = true;
}
