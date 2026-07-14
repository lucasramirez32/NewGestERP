using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.DTOs.Shared;

namespace NewGest.Application.Features.Comprobantes.Queries.GetComprobantes;

public record GetComprobantesQuery(int IdEmpresa, int Pagina, int Tamano) : IRequest<PagedResult<ComprobanteDto>>;
