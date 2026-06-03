using MediatR;
using NewGest.Application.DTOs.Articulos;

namespace NewGest.Application.Features.Articulos.Commands.CrearArticulo;

public record CrearArticuloCommand(
    int IdEmpresa,
    string Codigo,
    string Descripcion,
    int IdGrupo,
    int IdUnidad,
    decimal PrecioLista,
    decimal PrecioCosto,
    decimal PorcentajeIva,
    string? Observaciones
) : IRequest<ArticuloDto>;
