using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Articulos.Commands.DesactivarArticulo;

public class DesactivarArticuloCommandHandler : IRequestHandler<DesactivarArticuloCommand>
{
    private readonly IArticuloRepository _repo;
    private readonly IUnitOfWork _uow;

    public DesactivarArticuloCommandHandler(IArticuloRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DesactivarArticuloCommand request, CancellationToken cancellationToken)
    {
        var articulo = await _repo.GetByIdAsync(request.IdEmpresa, request.IdArticulo, cancellationToken)
            ?? throw new DomainException($"Artículo {request.IdArticulo} no encontrado.");

        articulo.Desactivar();
        _repo.Update(articulo);
        await _uow.CommitAsync(cancellationToken);
    }
}
