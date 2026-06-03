using MediatR;
using NewGest.Application.DTOs.Stock;

namespace NewGest.Application.Features.Stock.Queries.GetDepositos;

public record GetDepositosQuery(int IdEmpresa) : IRequest<IReadOnlyList<DepositoDto>>;
