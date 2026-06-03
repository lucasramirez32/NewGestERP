using MediatR;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Application.Features.Stock.Commands.CrearDeposito;

public class CrearDepositoCommandHandler : IRequestHandler<CrearDepositoCommand, DepositoDto>
{
    private readonly IDepositoRepository _repo;
    private readonly IUnitOfWork _uow;

    public CrearDepositoCommandHandler(IDepositoRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<DepositoDto> Handle(CrearDepositoCommand request, CancellationToken ct)
    {
        var deposito = new Deposito
        {
            IdEmpresa = request.IdEmpresa,
            Codigo = request.Codigo.Trim().ToUpper(),
            Descripcion = request.Descripcion.Trim(),
            Activo = true
        };

        await _repo.AddAsync(deposito, ct);
        await _uow.CommitAsync(ct);

        return new DepositoDto(deposito.IdDeposito, deposito.Codigo, deposito.Descripcion, deposito.Activo);
    }
}
