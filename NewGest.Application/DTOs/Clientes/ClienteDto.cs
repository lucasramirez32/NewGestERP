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
    bool Activo
);
