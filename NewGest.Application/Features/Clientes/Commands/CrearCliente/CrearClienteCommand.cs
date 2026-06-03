using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Clientes.Commands.CrearCliente;

public record CrearClienteCommand(
    int IdEmpresa,
    string Codigo,
    string RazonSocial,
    string? CUIT,
    CondicionIva CondicionIva,
    string? Domicilio,
    string? Localidad,
    string? Telefono,
    string? Email,
    int? IdZona,
    string? Observaciones
) : IRequest<ClienteDto>;
