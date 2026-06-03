using MediatR;
using NewGest.Application.DTOs.Stock;

namespace NewGest.Application.Features.Stock.Queries.GetExistencias;

public record GetExistenciasQuery(int IdEmpresa, int IdArticulo) : IRequest<IReadOnlyList<ExistenciaDepositoDto>>;
