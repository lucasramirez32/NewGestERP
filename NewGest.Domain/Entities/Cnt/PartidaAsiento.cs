using NewGest.Domain.Common;

namespace NewGest.Domain.Entities.Cnt;

public class PartidaAsiento
{
    public int IdPartida { get; private set; }
    public int IdAsiento { get; private set; }
    public int IdCuenta { get; private set; }
    public decimal Debe { get; private set; }
    public decimal Haber { get; private set; }
    public string? Concepto { get; private set; }

    private PartidaAsiento() { }

    public static PartidaAsiento Crear(int idCuenta, decimal debe, decimal haber, string? concepto = null)
    {
        if (debe < 0) throw new DomainException("El importe Debe no puede ser negativo.");
        if (haber < 0) throw new DomainException("El importe Haber no puede ser negativo.");
        if (debe == 0 && haber == 0) throw new DomainException("Una partida no puede tener Debe y Haber en cero.");

        return new PartidaAsiento
        {
            IdCuenta = idCuenta,
            Debe = Math.Round(debe, 2),
            Haber = Math.Round(haber, 2),
            Concepto = concepto
        };
    }
}
