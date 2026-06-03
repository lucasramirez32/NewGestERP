using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Clientes.Commands.DesactivarCliente;

public class DesactivarClienteCommandHandler : IRequestHandler<DesactivarClienteCommand>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesactivarClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DesactivarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _repo.GetByIdAsync(request.IdEmpresa, request.IdCliente, cancellationToken)
            ?? throw new DomainException($"Cliente {request.IdCliente} no encontrado.");

        cliente.Desactivar();
        _repo.Update(cliente);
        await _uow.CommitAsync(cancellationToken);
    }
}
