using NewGest.Domain.Common;
using NewGest.Domain.Enums;
using NewGest.Domain.Services;

namespace NewGest.Domain.Entities.Neg;

public class Empleado
{
    public int IdEmpleado { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Legajo { get; private set; } = default!;
    public string ApellidoNombre { get; private set; } = default!;
    public string? CUIL { get; private set; }
    public RolEmpleado Rol { get; private set; }
    public bool EsVendedor { get; private set; }
    public decimal? ComisionPorcentaje { get; private set; }
    public bool Activo { get; private set; } = true;

    private Empleado() { }

    public static Empleado Crear(
        int idEmpresa,
        string legajo,
        string apellidoNombre,
        string? cuil,
        RolEmpleado rol,
        decimal? comisionPorcentaje)
    {
        if (!string.IsNullOrWhiteSpace(cuil) && !CuitValidator.EsValido(cuil))
            throw new DomainException($"CUIL inválido: {cuil}");
        if (comisionPorcentaje.HasValue && (comisionPorcentaje < 0 || comisionPorcentaje > 100))
            throw new DomainException("El porcentaje de comisión debe estar entre 0 y 100.");

        return new Empleado
        {
            IdEmpresa = idEmpresa,
            Legajo = legajo.Trim().ToUpper(),
            ApellidoNombre = apellidoNombre.Trim(),
            CUIL = cuil?.Replace("-", "").Replace(" ", ""),
            Rol = rol,
            EsVendedor = rol == RolEmpleado.Vendedor,
            ComisionPorcentaje = comisionPorcentaje,
            Activo = true
        };
    }

    public void Actualizar(
        string apellidoNombre,
        string? cuil,
        RolEmpleado rol,
        decimal? comisionPorcentaje)
    {
        if (!string.IsNullOrWhiteSpace(cuil) && !CuitValidator.EsValido(cuil))
            throw new DomainException($"CUIL inválido: {cuil}");
        if (comisionPorcentaje.HasValue && (comisionPorcentaje < 0 || comisionPorcentaje > 100))
            throw new DomainException("El porcentaje de comisión debe estar entre 0 y 100.");

        ApellidoNombre = apellidoNombre.Trim();
        CUIL = cuil?.Replace("-", "").Replace(" ", "");
        Rol = rol;
        EsVendedor = rol == RolEmpleado.Vendedor;
        ComisionPorcentaje = comisionPorcentaje;
    }

    public void Desactivar() => Activo = false;
}
