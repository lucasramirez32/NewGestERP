using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Clientes.Queries.GetClientes;

public record GetClientesQuery(
    int IdEmpresa,
    string? Search,
    CondicionIva? CondicionIva,
    int? IdZona,
    int Page,
    int PageSize
) : IRequest<PagedResult<ClienteListItemDto>>;
