using MediatR;

namespace NewGest.Application.Features.Pedidos.Commands.AnularPedido;

public record AnularPedidoCommand(int IdEmpresa, int IdPedido) : IRequest;
