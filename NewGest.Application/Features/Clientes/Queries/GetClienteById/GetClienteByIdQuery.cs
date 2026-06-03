using MediatR;
using NewGest.Application.DTOs.Clientes;

namespace NewGest.Application.Features.Clientes.Queries.GetClienteById;

public record GetClienteByIdQuery(int IdEmpresa, int IdCliente) : IRequest<ClienteDto?>;
