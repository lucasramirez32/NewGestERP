using MediatR;
using NewGest.Application.DTOs.Contabilidad;

namespace NewGest.Application.Features.Contabilidad.Queries.GetLibroIva;

public enum TipoLibroIva { Ventas = 1, Compras = 2 }

public record GetLibroIvaQuery(int IdEmpresa, int Anio, int Mes, TipoLibroIva TipoLibro)
    : IRequest<LibroIvaDto>;
