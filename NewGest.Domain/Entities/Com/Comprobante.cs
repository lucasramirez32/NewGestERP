using NewGest.Domain.Common;
using NewGest.Domain.Enums;
using NewGest.Domain.Events;

namespace NewGest.Domain.Entities.Com;

public class Comprobante : AggregateRoot
{
    public int IdComprobante { get; private set; }
    public int IdEmpresa { get; private set; }
    public TipoComprobante Tipo { get; private set; }
    public int PuntoVenta { get; private set; }
    public long Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public int IdCliente { get; private set; }
    public string RazonSocialCliente { get; private set; } = default!;
    public string? CuitCliente { get; private set; }
    public CondicionIva CondicionIvaReceptor { get; private set; }
    public int? IdPedidoOrigen { get; private set; }

    public decimal TotalNeto { get; private set; }
    public decimal TotalIva { get; private set; }
    public decimal Total { get; private set; }

    public CaeInfo? Cae { get; private set; }
    public bool EsElectronica => Cae is not null;
    public bool Anulado { get; private set; }

    private readonly List<ItemComprobante> _items = [];
    public IReadOnlyList<ItemComprobante> Items => _items.AsReadOnly();

    private Comprobante() { }

    public static Comprobante Crear(
        int idEmpresa,
        TipoComprobante tipo,
        int puntoVenta,
        long numero,
        DateOnly fecha,
        int idCliente,
        string razonSocialCliente,
        string? cuitCliente,
        CondicionIva condicionIvaReceptor,
        IEnumerable<ItemComprobante> items,
        int? idPedidoOrigen = null)
    {
        ValidarTipoSegunCondicion(tipo, condicionIvaReceptor);

        var listaItems = items.ToList();
        if (listaItems.Count == 0)
            throw new DomainException("El comprobante debe tener al menos un ítem.");

        var neto = listaItems.Sum(i => i.SubtotalNeto);
        var iva = listaItems.Sum(i => i.Iva);

        var comprobante = new Comprobante
        {
            IdEmpresa = idEmpresa,
            Tipo = tipo,
            PuntoVenta = puntoVenta,
            Numero = numero,
            Fecha = fecha,
            IdCliente = idCliente,
            RazonSocialCliente = razonSocialCliente.Trim(),
            CuitCliente = cuitCliente,
            CondicionIvaReceptor = condicionIvaReceptor,
            IdPedidoOrigen = idPedidoOrigen,
            TotalNeto = Math.Round(neto, 2),
            TotalIva = Math.Round(iva, 2),
            Total = Math.Round(neto + iva, 2)
        };
        comprobante._items.AddRange(listaItems);
        return comprobante;
    }

    public void AsignarCae(string codigoCae, DateOnly fechaVencimiento)
    {
        if (Cae is not null)
            throw new DomainException("El comprobante ya tiene un CAE asignado.");

        Cae = new CaeInfo(codigoCae, fechaVencimiento);
        AddDomainEvent(new CaeAsignadoEvent(IdComprobante, codigoCae));
    }

    public void Anular()
    {
        if (Anulado)
            throw new DomainException("El comprobante ya está anulado.");
        Anulado = true;
    }

    private static void ValidarTipoSegunCondicion(TipoComprobante tipo, CondicionIva condicion)
    {
        var esA = tipo is TipoComprobante.FacturaA or TipoComprobante.NotaCreditoA or TipoComprobante.NotaDebitoA;

        if (esA && condicion is not CondicionIva.Inscripto)
            throw new DomainException(
                $"Los comprobantes tipo A solo pueden emitirse a Responsables Inscriptos. Condición: {condicion}.");
    }
}
