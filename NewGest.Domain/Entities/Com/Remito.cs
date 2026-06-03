using NewGest.Domain.Common;

namespace NewGest.Domain.Entities.Com;

public class Remito
{
    public int IdRemito { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdPedido { get; private set; }
    public int IdCliente { get; private set; }
    public DateTime FechaRemito { get; private set; }
    public string? Observaciones { get; private set; }
    public List<ItemRemito> Items { get; private set; } = [];

    private Remito() { }

    /// <summary>
    /// Genera un remito a partir de un pedido y una lista de despachos (idArticulo, cantidad).
    /// Actualiza CantidadEntregada en cada ItemPedido y llama a ActualizarEstado() sobre el pedido.
    /// </summary>
    public static Remito Crear(Pedido pedido, IEnumerable<(int IdArticulo, decimal Cantidad)> itemsADespachar)
    {
        var items = itemsADespachar.ToList();
        if (items.Count == 0) throw new DomainException("El remito debe tener al menos un artículo.");

        var remitoItems = new List<ItemRemito>();

        foreach (var (idArticulo, cantidad) in items)
        {
            var itemPedido = pedido.Items.FirstOrDefault(i => i.IdArticulo == idArticulo)
                ?? throw new DomainException($"El artículo {idArticulo} no pertenece al pedido.");

            if (cantidad <= 0)
                throw new DomainException("La cantidad a despachar debe ser mayor a cero.");

            if (cantidad > itemPedido.CantidadPendiente)
                throw new DomainException(
                    $"Cantidad a despachar ({cantidad}) supera la pendiente ({itemPedido.CantidadPendiente}) para el artículo {idArticulo}.");

            itemPedido.CantidadEntregada += cantidad;
            remitoItems.Add(new ItemRemito
            {
                IdArticulo = idArticulo,
                Cantidad = cantidad,
                PrecioUnitario = itemPedido.PrecioUnitario
            });
        }

        pedido.ActualizarEstado();

        return new Remito
        {
            IdEmpresa = pedido.IdEmpresa,
            IdPedido = pedido.IdPedido,
            IdCliente = pedido.IdCliente,
            FechaRemito = DateTime.UtcNow,
            Items = remitoItems
        };
    }
}
