using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Com;

public class MedioPago
{
    public int IdMedioPago { get; private set; }
    public int IdPago { get; private set; }
    public TipoMedioPago Tipo { get; private set; }
    public decimal Monto { get; private set; }
    public string? BancoEmisor { get; private set; }
    public string? NumeroCheque { get; private set; }
    public DateOnly? FechaVencimientoCheque { get; private set; }
    public string? NumeroTransferencia { get; private set; }

    private MedioPago() { }

    public static MedioPago Crear(TipoMedioPago tipo, decimal monto,
        string? bancoEmisor = null, string? numeroCheque = null,
        DateOnly? fechaVencCheque = null, string? numeroTransferencia = null)
    {
        if (monto <= 0) throw new DomainException("El monto del medio de pago debe ser mayor a cero.");

        return new MedioPago
        {
            Tipo = tipo, Monto = Math.Round(monto, 2),
            BancoEmisor = bancoEmisor, NumeroCheque = numeroCheque,
            FechaVencimientoCheque = fechaVencCheque,
            NumeroTransferencia = numeroTransferencia
        };
    }
}
