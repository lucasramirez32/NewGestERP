using NewGest.Domain.Common;

namespace NewGest.Domain.Entities.Com;

public class Imputacion
{
    public int IdImputacion { get; private set; }
    public int IdPago { get; private set; }
    public int IdComprobante { get; private set; }
    public decimal Monto { get; private set; }

    private Imputacion() { }

    public static Imputacion Crear(int idComprobante, decimal monto)
    {
        if (monto <= 0) throw new DomainException("El monto imputado debe ser mayor a cero.");
        return new Imputacion { IdComprobante = idComprobante, Monto = Math.Round(monto, 2) };
    }
}
