using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Queries.GetViajes;

public record GetViajesQuery(int IdEmpresa) : IRequest<IReadOnlyList<ViajeDto>>;
