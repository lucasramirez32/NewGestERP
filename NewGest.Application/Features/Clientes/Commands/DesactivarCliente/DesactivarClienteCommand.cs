using MediatR;

namespace NewGest.Application.Features.Clientes.Commands.DesactivarCliente;

public record DesactivarClienteCommand(int IdEmpresa, int IdCliente) : IRequest;
