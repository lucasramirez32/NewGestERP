using NewGest.Domain.Enums;

namespace NewGest.Application.DTOs.Clientes;

public record ClienteListItemDto(
    int IdCliente,
    string Codigo,
    string RazonSocial,
    string? CUIT,
    CondicionIva CondicionIva,
    string CondicionIvaDescripcion,
    string? Localidad,
    int? IdZona,
    bool Activo
);
