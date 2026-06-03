using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Queries.GetMutuales;

public record GetMutualesQuery(int IdEmpresa) : IRequest<IReadOnlyList<MutualDto>>;
