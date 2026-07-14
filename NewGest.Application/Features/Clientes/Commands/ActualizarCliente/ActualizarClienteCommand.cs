using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Clientes.Commands.ActualizarCliente;

public record ActualizarClienteCommand(
    int IdEmpresa,
    int IdCliente,
    string RazonSocial,
    string? CUIT,
    CondicionIva CondicionIva,
    string? Domicilio,
    string? Localidad,
    string? Telefono,
    string? Email,
    int? IdZona,
    string? Observaciones,
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
) : IRequest<ClienteDto>;
