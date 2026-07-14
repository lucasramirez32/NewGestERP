using MediatR;
using NewGest.Application.DTOs.Articulos;

namespace NewGest.Application.Features.Articulos.Commands.ActualizarArticulo;

public record ActualizarArticuloCommand(
    int IdEmpresa,
    int IdArticulo,
    string Descripcion,
    int? IdGrupo,
    int IdUnidad,
    decimal PrecioLista,
    decimal PrecioCosto,
    decimal PorcentajeIva,
    string? Observaciones = null,
    string? NombreGrupo = null
) : IRequest<ArticuloDto>;
