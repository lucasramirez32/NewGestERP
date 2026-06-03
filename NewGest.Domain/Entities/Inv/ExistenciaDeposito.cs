namespace NewGest.Domain.Entities.Inv;

public class ExistenciaDeposito
{
    public int IdExistencia { get; set; }
    public int IdEmpresa { get; set; }
    public int IdArticulo { get; set; }
    public int IdDeposito { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal StockMinimo { get; set; }
}
