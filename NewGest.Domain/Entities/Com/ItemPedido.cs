namespace NewGest.Domain.Entities.Com;

public class ItemPedido
{
    public int IdItemPedido { get; set; }
    public int IdPedido { get; set; }
    public int IdArticulo { get; set; }
    public decimal CantidadPedida { get; set; }
    public decimal CantidadEntregada { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal CantidadPendiente => CantidadPedida - CantidadEntregada;
}
