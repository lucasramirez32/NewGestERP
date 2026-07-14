using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.Features.Clientes.Commands.CrearCliente;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Clientes.Commands.ActualizarCliente;

public class ActualizarClienteCommandHandler : IRequestHandler<ActualizarClienteCommand, ClienteDto>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;

    public ActualizarClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ClienteDto> Handle(ActualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _repo.GetByIdAsync(request.IdEmpresa, request.IdCliente, cancellationToken)
            ?? throw new DomainException($"Cliente {request.IdCliente} no encontrado.");

        if (!string.IsNullOrWhiteSpace(request.CUIT))
        {
            var cuitLimpio = request.CUIT.Replace("-", "").Replace(" ", "");
            if (await _repo.ExisteCuitAsync(request.IdEmpresa, cuitLimpio, request.IdCliente, cancellationToken))
                throw new DomainException($"Ya existe un cliente con el CUIT '{request.CUIT}'.");
        }

        cliente.Actualizar(
            request.RazonSocial,
            request.CUIT,
            request.CondicionIva,
            request.Domicilio,
            request.Localidad,
            request.Telefono,
            request.Email,
            request.IdZona,
            request.Observaciones,
            request.NombreFantasia,
            request.LimiteCredito,
            request.DiasMora,
            request.Descuento,
            request.Provincia,
            request.CodigoPostal,
            request.ObraSocial,
            request.NroAfiliado,
            request.MedicoCabecera,
            request.MatriculaMedico,
            request.Alergia,
            request.Alergias,
            request.Tratamiento,
            request.Convulsiones,
            request.Medicacion,
            request.Patologia);

        _repo.Update(cliente);
        await _uow.CommitAsync(cancellationToken);

        return CrearClienteCommandHandler.ToDto(cliente);
    }
}
