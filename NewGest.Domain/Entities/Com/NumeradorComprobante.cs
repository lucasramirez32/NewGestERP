using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class NumeradorComprobante
{
    public int IdNumerador { get; private set; }
    public int IdEmpresa { get; private set; }
    public int PuntoVenta { get; private set; }
    public TipoComprobante Tipo { get; private set; }
    public long UltimoNumero { get; private set; }

    private NumeradorComprobante() { }

    public static NumeradorComprobante Crear(int idEmpresa, int puntoVenta, TipoComprobante tipo) =>
        new() { IdEmpresa = idEmpresa, PuntoVenta = puntoVenta, Tipo = tipo, UltimoNumero = 0 };
}
