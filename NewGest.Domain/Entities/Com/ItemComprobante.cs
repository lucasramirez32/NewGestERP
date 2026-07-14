using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class ItemComprobante
{
    public int IdItemComprobante { get; private set; }
    public int IdComprobante { get; private set; }
    public int IdArticulo { get; private set; }
    public string Descripcion { get; private set; } = default!;
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public AlicuotaIva Alicuota { get; private set; }
    public decimal SubtotalNeto { get; private set; }
    public decimal Iva { get; private set; }
    public decimal Subtotal { get; private set; }

    private ItemComprobante() { }

    public static ItemComprobante Crear(
        int idArticulo,
        string descripcion,
        decimal cantidad,
        decimal precioUnitario,
        AlicuotaIva alicuota)
    {
        var porcentaje = ObtenerPorcentaje(alicuota);
        var neto = cantidad * precioUnitario;
        var iva = neto * porcentaje / 100m;

        return new ItemComprobante
        {
            IdArticulo = idArticulo,
            Descripcion = descripcion.Trim(),
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Alicuota = alicuota,
            SubtotalNeto = neto,
            Iva = Math.Round(iva, 2),
            Subtotal = Math.Round(neto + iva, 2)
        };
    }

    public static decimal ObtenerPorcentaje(AlicuotaIva alicuota) => alicuota switch
    {
        AlicuotaIva.Porcentaje0 => 0m,
        AlicuotaIva.Porcentaje2_5 => 2.5m,
        AlicuotaIva.Porcentaje5 => 5m,
        AlicuotaIva.Porcentaje10_5 => 10.5m,
        AlicuotaIva.Porcentaje21 => 21m,
        AlicuotaIva.Porcentaje27 => 27m,
        AlicuotaIva.Exento => 0m,
        _ => 0m
    };
}
