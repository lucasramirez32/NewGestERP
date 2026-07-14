using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class Pago : AggregateRoot
{
    public int IdPago { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdCliente { get; private set; }
    public long Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public decimal TotalMedios { get; private set; }
    public decimal TotalImputado { get; private set; }
    public decimal SaldoAFavor { get; private set; }
    public string? Observaciones { get; private set; }
    public bool Anulado { get; private set; }

    private readonly List<MedioPago>  _medios       = [];
    private readonly List<Imputacion> _imputaciones = [];
    private readonly List<Retencion>  _retenciones  = [];

    public IReadOnlyList<MedioPago>  Medios       => _medios.AsReadOnly();
    public IReadOnlyList<Imputacion> Imputaciones => _imputaciones.AsReadOnly();
    public IReadOnlyList<Retencion>  Retenciones  => _retenciones.AsReadOnly();

    private Pago() { }

    public static Pago Crear(int idEmpresa, int idCliente, long numero, DateOnly fecha,
        IEnumerable<MedioPago> medios, string? observaciones = null)
    {
        var listaMedios = medios.ToList();
        if (listaMedios.Count == 0)
            throw new DomainException("El pago debe tener al menos un medio de pago.");

        var total = listaMedios.Sum(m => m.Monto);

        var pago = new Pago
        {
            IdEmpresa = idEmpresa, IdCliente = idCliente,
            Numero = numero, Fecha = fecha,
            TotalMedios = Math.Round(total, 2), Observaciones = observaciones
        };
        pago._medios.AddRange(listaMedios);
        return pago;
    }

    public void ImputarComprobante(int idComprobante, decimal monto)
    {
        if (Anulado) throw new DomainException("No se puede imputar un pago anulado.");

        var nuevoTotal = TotalImputado + monto;
        if (nuevoTotal > TotalMedios)
            throw new DomainException(
                $"La imputación supera el total cobrado. Disponible: ${TotalMedios - TotalImputado:N2}");

        _imputaciones.Add(Imputacion.Crear(idComprobante, monto));
        TotalImputado = Math.Round(nuevoTotal, 2);
        SaldoAFavor   = Math.Round(TotalMedios - TotalImputado, 2);
    }

    public void AgregarRetencion(Retencion retencion)
    {
        if (Anulado) throw new DomainException("No se puede agregar retención a un pago anulado.");
        _retenciones.Add(retencion);
    }

    public void Anular()
    {
        if (Anulado) throw new DomainException("El pago ya está anulado.");
        Anulado = true;
    }
}
