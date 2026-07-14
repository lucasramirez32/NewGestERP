using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Clientes;

public record CrearClienteDto(
    string Codigo,
    string RazonSocial,
    string? CUIT,
    CondicionIva CondicionIva,
    string? Domicilio = null,
    string? Localidad = null,
    string? Telefono = null,
    string? Email = null,
    int? IdZona = null,
    string? Observaciones = null,
    string? NombreFantasia = null,
    decimal LimiteCredito = 0m,
    int DiasMora = 0,
    decimal Descuento = 0m,
    string? Provincia = null,
    string? CodigoPostal = null,
    string? ObraSocial = null,
    string? NroAfiliado = null,
    string? MedicoCabecera = null,
    string? MatriculaMedico = null,
    bool Alergia = false,
    string? Alergias = null,
    bool Tratamiento = false,
    bool Convulsiones = false,
    string? Medicacion = null,
    string? Patologia = null
);
