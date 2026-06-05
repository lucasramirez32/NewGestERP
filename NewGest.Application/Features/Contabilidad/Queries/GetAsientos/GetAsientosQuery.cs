using MediatR;
using NewGest.Application.DTOs.Contabilidad;
using NewGest.Application.DTOs.Shared;

namespace NewGest.Application.Features.Contabilidad.Queries.GetAsientos;

public record GetAsientosQuery(int IdEmpresa, int Pagina, int Tamano) : IRequest<PagedResult<AsientoDto>>;
