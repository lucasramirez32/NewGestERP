using NewGest.Domain.Common;
using NewGest.Domain.Enums;

namespace NewGest.Domain.Entities.Cnt;

public class CuentaContable
{
    public int IdCuenta { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public int? IdCuentaPadre { get; private set; }
    public NaturalezaCuenta Naturaleza { get; private set; }
    public TipoCuenta Tipo { get; private set; }
    public bool ImputaDirectamente { get; private set; }
    public bool Activa { get; private set; } = true;

    private CuentaContable() { }

    public static CuentaContable Crear(
        int idEmpresa, string codigo, string descripcion,
        int? idCuentaPadre, NaturalezaCuenta naturaleza,
        TipoCuenta tipo, bool imputaDirectamente)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new DomainException("El código de cuenta no puede estar vacío.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DomainException("La descripción de cuenta no puede estar vacía.");

        return new CuentaContable
        {
            IdEmpresa = idEmpresa,
            Codigo = codigo.Trim(),
            Descripcion = descripcion.Trim(),
            IdCuentaPadre = idCuentaPadre,
            Naturaleza = naturaleza,
            Tipo = tipo,
            ImputaDirectamente = imputaDirectamente,
            Activa = true
        };
    }

    public void Actualizar(string descripcion, NaturalezaCuenta naturaleza, TipoCuenta tipo, bool imputaDirectamente)
    {
        Descripcion = descripcion.Trim();
        Naturaleza = naturaleza;
        Tipo = tipo;
        ImputaDirectamente = imputaDirectamente;
    }

    public void Desactivar() => Activa = false;
    public void Activar() => Activa = true;
}
