using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Inv;

public class MovimientoStock
{
    public int IdMovimiento { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdArticulo { get; private set; }
    public int IdDeposito { get; private set; }
    public TipoMovimiento Tipo { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public string? NumeroSerie { get; private set; }
    public int? IdComprobanteOrigen { get; private set; }
    public DateTime FechaMovimiento { get; private set; }
    public string? Observaciones { get; private set; }

    private MovimientoStock() { }

    public static MovimientoStock Crear(
        int idEmpresa,
        int idArticulo,
        int idDeposito,
        TipoMovimiento tipo,
        decimal cantidad,
        decimal costoUnitario,
        string? numeroSerie = null,
        int? idComprobanteOrigen = null,
        string? observaciones = null)
    {
        if (cantidad <= 0) throw new DomainException("La cantidad debe ser mayor a cero.");
        if (costoUnitario < 0) throw new DomainException("El costo unitario no puede ser negativo.");

        return new MovimientoStock
        {
            IdEmpresa = idEmpresa,
            IdArticulo = idArticulo,
            IdDeposito = idDeposito,
            Tipo = tipo,
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            NumeroSerie = numeroSerie,
            IdComprobanteOrigen = idComprobanteOrigen,
            FechaMovimiento = DateTime.UtcNow,
            Observaciones = observaciones
        };
    }
}
