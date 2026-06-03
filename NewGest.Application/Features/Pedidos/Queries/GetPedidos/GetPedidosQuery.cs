using MediatR;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Pedidos.Queries.GetPedidos;

public record GetPedidosQuery(
    int IdEmpresa,
    EstadoPedido? Estado,
    int Page,
    int PageSize) : IRequest<PagedResult<PedidoListItemDto>>;
