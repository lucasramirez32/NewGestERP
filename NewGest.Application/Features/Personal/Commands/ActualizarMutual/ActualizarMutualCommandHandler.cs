using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Features.Personal.Commands.CrearMutual;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Personal.Commands.ActualizarMutual;

public class ActualizarMutualCommandHandler : IRequestHandler<ActualizarMutualCommand, MutualDto>
{
    private readonly IMutualRepository _repo;
    private readonly IUnitOfWork _uow;

    public ActualizarMutualCommandHandler(IMutualRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<MutualDto> Handle(ActualizarMutualCommand request, CancellationToken ct)
    {
        var mutual = await _repo.GetByIdAsync(request.IdEmpresa, request.IdMutual, ct)
            ?? throw new DomainException($"Mutual {request.IdMutual} no encontrada.");

        mutual.Descripcion = request.Datos.Descripcion.Trim();
        _repo.Update(mutual);
        await _uow.CommitAsync(ct);

        return CrearMutualCommandHandler.ToDto(mutual);
    }
}
