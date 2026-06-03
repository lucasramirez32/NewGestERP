using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Clientes.Commands.CrearCliente;

public class CrearClienteCommandHandler : IRequestHandler<CrearClienteCommand, ClienteDto>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ClienteDto> Handle(CrearClienteCommand request, CancellationToken cancellationToken)
    {
        if (await _repo.ExisteCodigoAsync(request.IdEmpresa, request.Codigo, null, cancellationToken))
            throw new DomainException($"Ya existe un cliente con el código '{request.Codigo}'.");

        if (!string.IsNullOrWhiteSpace(request.CUIT))
        {
            var cuitLimpio = request.CUIT.Replace("-", "").Replace(" ", "");
            if (await _repo.ExisteCuitAsync(request.IdEmpresa, cuitLimpio, null, cancellationToken))
                throw new DomainException($"Ya existe un cliente con el CUIT '{request.CUIT}'.");
        }

        var cliente = Cliente.Crear(
            request.IdEmpresa,
            request.Codigo,
            request.RazonSocial,
            request.CUIT,
            request.CondicionIva,
            request.Domicilio,
            request.Localidad,
            request.Telefono,
            request.Email,
            request.IdZona,
            request.Observaciones);

        await _repo.AddAsync(cliente, cancellationToken);
        await _uow.CommitAsync(cancellationToken);

        return ToDto(cliente);
    }

    internal static ClienteDto ToDto(Cliente c) => new(
        c.IdCliente,
        c.IdEmpresa,
        c.Codigo,
        c.RazonSocial,
        c.CUIT,
        c.CondicionIva,
        c.CondicionIva.ToString(),
        c.Domicilio,
        c.Localidad,
        c.Telefono,
        c.Email,
        c.IdZona,
        c.Observaciones,
        c.Activo);
}
