using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.Features.Clientes.Commands.CrearCliente;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Clientes.Queries.GetClienteById;

public class GetClienteByIdQueryHandler : IRequestHandler<GetClienteByIdQuery, ClienteDto?>
{
    private readonly IClienteRepository _repo;

    public GetClienteByIdQueryHandler(IClienteRepository repo)
    {
        _repo = repo;
    }

    public async Task<ClienteDto?> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _repo.GetByIdAsync(request.IdEmpresa, request.IdCliente, cancellationToken);
        return cliente is null ? null : CrearClienteCommandHandler.ToDto(cliente);
    }
}
