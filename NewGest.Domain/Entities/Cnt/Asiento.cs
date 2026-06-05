using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Cnt;

public class Asiento : AggregateRoot
{
    public int IdAsiento { get; private set; }
    public int IdEmpresa { get; private set; }
    public long Numero { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string Descripcion { get; private set; } = default!;
    public TipoAsiento TipoAsiento { get; private set; }
    public int? IdComprobanteOrigen { get; private set; }
    public bool Anulado { get; private set; }

    private readonly List<PartidaAsiento> _partidas = [];
    public IReadOnlyList<PartidaAsiento> Partidas => _partidas.AsReadOnly();

    public decimal TotalDebe => _partidas.Sum(p => p.Debe);
    public decimal TotalHaber => _partidas.Sum(p => p.Haber);

    private Asiento() { }

    public static Asiento Crear(
        int idEmpresa, long numero, DateOnly fecha, string descripcion,
        TipoAsiento tipo, int? idComprobanteOrigen = null)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DomainException("El asiento debe tener una descripción.");

        return new Asiento
        {
            IdEmpresa = idEmpresa,
            Numero = numero,
            Fecha = fecha,
            Descripcion = descripcion.Trim(),
            TipoAsiento = tipo,
            IdComprobanteOrigen = idComprobanteOrigen
        };
    }

    public void AgregarPartida(int idCuenta, decimal debe, decimal haber, string? concepto = null)
    {
        if (Anulado) throw new DomainException("No se pueden agregar partidas a un asiento anulado.");
        _partidas.Add(PartidaAsiento.Crear(idCuenta, debe, haber, concepto));
    }

    /// <summary>
    /// Verifica que el asiento esté en equilibrio (doble partida: Debe = Haber).
    /// Debe llamarse antes de persistir. Un centavo de diferencia es error.
    /// </summary>
    public void Validar()
    {
        if (_partidas.Count < 2)
            throw new DomainException("Un asiento debe tener al menos 2 partidas.");

        if (TotalDebe != TotalHaber)
            throw new DomainException(
                $"Asiento desequilibrado: Debe={TotalDebe:N2} / Haber={TotalHaber:N2} " +
                $"(diferencia={Math.Abs(TotalDebe - TotalHaber):N2})");
    }

    public void Anular()
    {
        if (Anulado) throw new DomainException("El asiento ya está anulado.");
        if (TipoAsiento != TipoAsiento.Manual)
            throw new DomainException("Solo se pueden anular asientos manuales.");
        Anulado = true;
    }
}
