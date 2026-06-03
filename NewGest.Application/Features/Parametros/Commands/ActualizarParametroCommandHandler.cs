using MediatR;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;

namespace NewGest.Application.Features.Parametros.Commands;

public class ActualizarParametroCommandHandler : IRequestHandler<ActualizarParametroCommand, Unit>
{
    private readonly IParametroRepository _parametroRepo;

    public ActualizarParametroCommandHandler(IParametroRepository parametroRepo)
    {
        _parametroRepo = parametroRepo;
    }

    public async Task<Unit> Handle(ActualizarParametroCommand request, CancellationToken cancellationToken)
    {
        var existente = await _parametroRepo.ObtenerPorClaveAsync(request.Clave, request.IdEmpresa, cancellationToken)
            ?? throw new DomainException($"No existe el parámetro '{request.Clave}'.");

        await _parametroRepo.ActualizarValorAsync(request.Clave, request.IdEmpresa, request.NuevoValor, cancellationToken);

        return Unit.Value;
    }
}
