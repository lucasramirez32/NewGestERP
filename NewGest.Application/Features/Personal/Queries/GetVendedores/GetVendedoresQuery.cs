using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;

namespace NewGest.Application.Features.Personal.Queries.GetVendedores;

public record GetVendedoresQuery(int IdEmpresa) : IRequest<IReadOnlyList<EmpleadoListItemDto>>;
