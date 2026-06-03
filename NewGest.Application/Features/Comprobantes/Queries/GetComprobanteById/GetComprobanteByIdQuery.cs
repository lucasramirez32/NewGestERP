using MediatR;
using NewGest.Application.DTOs.Comprobantes;

namespace NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;

public record GetComprobanteByIdQuery(int IdComprobante, int IdEmpresa) : IRequest<ComprobanteDto?>;
