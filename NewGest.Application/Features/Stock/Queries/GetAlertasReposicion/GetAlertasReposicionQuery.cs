using MediatR;
using NewGest.Application.DTOs.Stock;

namespace NewGest.Application.Features.Stock.Queries.GetAlertasReposicion;

public record GetAlertasReposicionQuery(int IdEmpresa) : IRequest<IReadOnlyList<ExistenciaDepositoDto>>;
