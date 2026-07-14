using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Clientes;

public record ClienteDto(
    int IdCliente,
    int IdEmpresa,
    string Codigo,
    string RazonSocial,
    string? CUIT,
    CondicionIva CondicionIva,
    string CondicionIvaDescripcion,
    string? Domicilio,
    string? Localidad,
    string? Telefono,
    string? Email,
    int? IdZona,
    string? Observaciones,
    bool Activo,
    string? NombreFantasia,
    decimal LimiteCredito,
    int DiasMora,
    decimal Descuento,
    string? Provincia,
    string? CodigoPostal,
    string? ObraSocial,
    string? NroAfiliado,
    string? MedicoCabecera,
    string? MatriculaMedico,
    bool Alergia,
    string? Alergias,
    bool Tratamiento,
    bool Convulsiones,
    string? Medicacion,
    string? Patologia
);
