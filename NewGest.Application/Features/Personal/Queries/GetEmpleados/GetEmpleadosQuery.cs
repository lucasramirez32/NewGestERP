using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;

namespace NewGest.Application.Features.Personal.Queries.GetEmpleados;

public record GetEmpleadosQuery(
    int IdEmpresa,
    string? Search,
    bool? SoloVendedores,
    int Page,
    int PageSize) : IRequest<PagedResult<EmpleadoListItemDto>>;
