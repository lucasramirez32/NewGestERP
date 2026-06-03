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
    string? Observaciones
) : IRequest<ClienteDto>;
