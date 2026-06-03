namespace NewGest.Domain.Entities.Com;

public class ItemRemito
{
    public int IdItemRemito { get; set; }
    public int IdRemito { get; set; }
    public int IdArticulo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
