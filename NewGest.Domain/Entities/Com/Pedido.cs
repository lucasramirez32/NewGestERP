using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class Pedido
{
    public int IdPedido { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdCliente { get; private set; }
    public int? IdVendedor { get; private set; }
    public EstadoPedido Estado { get; private set; } = EstadoPedido.Pendiente;
    public DateTime FechaPedido { get; private set; }
    public DateTime? FechaEntregaEstimada { get; private set; }
    public string? Observaciones { get; private set; }
    public List<ItemPedido> Items { get; private set; } = [];

    private Pedido() { }

    public static Pedido Crear(
        int idEmpresa,
        int idCliente,
        int? idVendedor,
        DateTime? fechaEntregaEstimada,
        string? observaciones,
        IEnumerable<(int IdArticulo, decimal Cantidad, decimal PrecioUnitario)> items)
    {
        var listaItems = items.ToList();
        if (listaItems.Count == 0)
            throw new DomainException("El pedido debe tener al menos un artículo.");

        return new Pedido
        {
            IdEmpresa = idEmpresa,
            IdCliente = idCliente,
            IdVendedor = idVendedor,
            FechaPedido = DateTime.UtcNow,
            FechaEntregaEstimada = fechaEntregaEstimada,
            Observaciones = observaciones,
            Estado = EstadoPedido.Pendiente,
            Items = listaItems.Select(i => new ItemPedido
            {
                IdArticulo = i.IdArticulo,
                CantidadPedida = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList()
        };
    }

    public void Anular()
    {
        if (Estado == EstadoPedido.Entregado)
            throw new DomainException("No se puede anular un pedido ya entregado.");
        Estado = EstadoPedido.Anulado;
    }

    public void ActualizarEstado()
    {
        if (Items.All(i => i.CantidadEntregada >= i.CantidadPedida))
            Estado = EstadoPedido.Entregado;
        else if (Items.Any(i => i.CantidadEntregada > 0))
            Estado = EstadoPedido.Parcial;
    }
}
