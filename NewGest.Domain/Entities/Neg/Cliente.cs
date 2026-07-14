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

    // Campos adicionales migrados de VFP
    public string? NombreFantasia { get; private set; }
    public decimal LimiteCredito { get; private set; }
    public int DiasMora { get; private set; }
    public decimal Descuento { get; private set; }
    public string? Provincia { get; private set; }
    public string? CodigoPostal { get; private set; }

    // Ficha Médica
    public string? ObraSocial { get; private set; }
    public string? NroAfiliado { get; private set; }
    public string? MedicoCabecera { get; private set; }
    public string? MatriculaMedico { get; private set; }
    public bool Alergia { get; private set; }
    public string? Alergias { get; private set; }
    public bool Tratamiento { get; private set; }
    public bool Convulsiones { get; private set; }
    public string? Medicacion { get; private set; }
    public string? Patologia { get; private set; }

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
        string? observaciones,
        string? nombreFantasia = null,
        decimal limiteCredito = 0m,
        int diasMora = 0,
        decimal descuento = 0m,
        string? provincia = null,
        string? codigoPostal = null,
        string? obraSocial = null,
        string? nroAfiliado = null,
        string? medicoCabecera = null,
        string? matriculaMedico = null,
        bool alergia = false,
        string? alergias = null,
        bool tratamiento = false,
        bool convulsiones = false,
        string? medicacion = null,
        string? patologia = null)
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
            Activo = true,

            NombreFantasia = nombreFantasia?.Trim(),
            LimiteCredito = limiteCredito,
            DiasMora = diasMora,
            Descuento = descuento,
            Provincia = provincia?.Trim(),
            CodigoPostal = codigoPostal?.Trim(),

            ObraSocial = obraSocial?.Trim(),
            NroAfiliado = nroAfiliado?.Trim(),
            MedicoCabecera = medicoCabecera?.Trim(),
            MatriculaMedico = matriculaMedico?.Trim(),
            Alergia = alergia,
            Alergias = alergias?.Trim(),
            Tratamiento = tratamiento,
            Convulsiones = convulsiones,
            Medicacion = medicacion?.Trim(),
            Patologia = patologia?.Trim()
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
        string? observaciones,
        string? nombreFantasia = null,
        decimal limiteCredito = 0m,
        int diasMora = 0,
        decimal descuento = 0m,
        string? provincia = null,
        string? codigoPostal = null,
        string? obraSocial = null,
        string? nroAfiliado = null,
        string? medicoCabecera = null,
        string? matriculaMedico = null,
        bool alergia = false,
        string? alergias = null,
        bool tratamiento = false,
        bool convulsiones = false,
        string? medicacion = null,
        string? patologia = null)
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

        NombreFantasia = nombreFantasia?.Trim();
        LimiteCredito = limiteCredito;
        DiasMora = diasMora;
        Descuento = descuento;
        Provincia = provincia?.Trim();
        CodigoPostal = codigoPostal?.Trim();

        ObraSocial = obraSocial?.Trim();
        NroAfiliado = nroAfiliado?.Trim();
        MedicoCabecera = medicoCabecera?.Trim();
        MatriculaMedico = matriculaMedico?.Trim();
        Alergia = alergia;
        Alergias = alergias?.Trim();
        Tratamiento = tratamiento;
        Convulsiones = convulsiones;
        Medicacion = medicacion?.Trim();
        Patologia = patologia?.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
