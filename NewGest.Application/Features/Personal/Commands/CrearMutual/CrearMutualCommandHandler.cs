using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Features.Personal.Commands.CrearMutual;

public class CrearMutualCommandHandler : IRequestHandler<CrearMutualCommand, MutualDto>
{
    private readonly IMutualRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearMutualCommandHandler(IMutualRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<MutualDto> Handle(CrearMutualCommand request, CancellationToken ct)
    {
        var dto = request.Datos;
        var mutual = new Mutual
        {
            IdEmpresa = request.IdEmpresa,
            Codigo = dto.Codigo.Trim().ToUpper(),
            Descripcion = dto.Descripcion.Trim(),
            Activo = true
        };

        await _repo.AddAsync(mutual, ct);
        await _uow.CommitAsync(ct);

        return ToDto(mutual);
    }

    internal static MutualDto ToDto(Mutual m) =>
        new(m.IdMutual, m.Codigo, m.Descripcion, m.Activo);
}
