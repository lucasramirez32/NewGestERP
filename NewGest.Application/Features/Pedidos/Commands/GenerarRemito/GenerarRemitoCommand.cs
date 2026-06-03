using MediatR;
using NewGest.Application.DTOs.Pedidos;

namespace NewGest.Application.Features.Pedidos.Commands.GenerarRemito;

public record GenerarRemitoCommand(int IdEmpresa, int IdPedido, GenerarRemitoDto Datos) : IRequest<RemitoDto>;
