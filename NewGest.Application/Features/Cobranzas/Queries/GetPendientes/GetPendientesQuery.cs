using MediatR;
using NewGest.Application.DTOs.Cobranzas;

namespace NewGest.Application.Features.Cobranzas.Queries.GetPendientes;

public record GetPendientesQuery(int IdEmpresa, int IdCliente) : IRequest<IReadOnlyList<ComprobantePendienteDto>>;
