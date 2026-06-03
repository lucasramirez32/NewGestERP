using MediatR;
using NewGest.Application.DTOs.Pedidos;

namespace NewGest.Application.Features.Pedidos.Queries.GetPedidoById;

public record GetPedidoByIdQuery(int IdEmpresa, int IdPedido) : IRequest<PedidoDto?>;
