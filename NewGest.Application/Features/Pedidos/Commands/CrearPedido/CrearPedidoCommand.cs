using MediatR;
using NewGest.Application.DTOs.Pedidos;

namespace NewGest.Application.Features.Pedidos.Commands.CrearPedido;

public record CrearPedidoCommand(int IdEmpresa, CrearPedidoDto Datos) : IRequest<PedidoDto>;
