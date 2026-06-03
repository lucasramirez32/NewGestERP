using NewGest.Domain.Common;
using NewGest.Domain.Enums;
using NewGest.Domain.Services;

namespace NewGest.Domain.Entities.Neg;

public class Cliente
{
    public int IdCliente { get; private set; }
    public int IdEmpresa { get; private set; }
    public string Codigo { get; private set; } = default!;
    public string RazonSocial { get; private set; } = default!;
    public string? CUIT { get; private set; }
    public CondicionIva CondicionIva { get; private set; }
    public string? Domicilio { get; private set; }
    public string? Localidad { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public int? IdZona { get; private set; }
    public string? Observaciones { get; private set; }
    public bool Activo { get; private set; } = true;

    private Cliente() { }

    public static Cliente Crear(
        int idEmpresa,
        string codigo,
        string razonSocial,
        string? cuit,
        CondicionIva condicionIva,
        string? domicilio,
        string? localidad,
        string? telefono,
        string? email,
        int? idZona,
        string? observaciones)
    {
        if (!string.IsNullOrWhiteSpace(cuit) && !CuitValidator.EsValido(cuit))
            throw new DomainException($"CUIT inválido: {cuit}");

        return new Cliente
        {
            IdEmpresa = idEmpresa,
            Codigo = codigo.Trim().ToUpper(),
            RazonSocial = razonSocial.Trim(),
            CUIT = cuit?.Replace("-", "").Replace(" ", ""),
            CondicionIva = condicionIva,
            Domicilio = domicilio,
            Localidad = localidad,
            Telefono = telefono,
            Email = email,
            IdZona = idZona,
            Observaciones = observaciones,
            Activo = true
        };
    }

    public void Actualizar(
        string razonSocial,
        string? cuit,
        CondicionIva condicionIva,
        string? domicilio,
        string? localidad,
        string? telefono,
        string? email,
        int? idZona,
        string? observaciones)
    {
        if (!string.IsNullOrWhiteSpace(cuit) && !CuitValidator.EsValido(cuit))
            throw new DomainException($"CUIT inválido: {cuit}");

        RazonSocial = razonSocial.Trim();
        CUIT = cuit?.Replace("-", "").Replace(" ", "");
        CondicionIva = condicionIva;
        Domicilio = domicilio;
        Localidad = localidad;
        Telefono = telefono;
        Email = email;
        IdZona = idZona;
        Observaciones = observaciones;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
