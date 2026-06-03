using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Clientes;

public record CrearClienteDto(
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
);
