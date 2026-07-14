using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class Retencion
{
    public int IdRetencion { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdPago { get; private set; }
    public TipoRetencion Tipo { get; private set; }
    public string? Provincia { get; private set; }
    public decimal Porcentaje { get; private set; }
    public decimal BaseImponible { get; private set; }
    public decimal MontoRetenido { get; private set; }
    public string NumeroFormulario { get; private set; } = default!;

    private Retencion() { }

    public static Retencion Crear(int idEmpresa, TipoRetencion tipo, string? provincia,
        decimal porcentaje, decimal baseImponible, string numeroFormulario)
    {
        var monto = Math.Round(baseImponible * porcentaje / 100m, 2);
        return new Retencion
        {
            IdEmpresa = idEmpresa, Tipo = tipo, Provincia = provincia,
            Porcentaje = porcentaje, BaseImponible = baseImponible,
            MontoRetenido = monto, NumeroFormulario = numeroFormulario
        };
    }
}
