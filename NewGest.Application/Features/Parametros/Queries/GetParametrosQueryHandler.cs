using MediatR;
using NewGest.Application.DTOs;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Parametros.Queries;

public class GetParametrosQueryHandler : IRequestHandler<GetParametrosQuery, IReadOnlyList<ParametroDto>>
{
    private readonly IParametroRepository _parametroRepo;

    public GetParametrosQueryHandler(IParametroRepository parametroRepo)
    {
        _parametroRepo = parametroRepo;
    }

    public async Task<IReadOnlyList<ParametroDto>> Handle(GetParametrosQuery request, CancellationToken cancellationToken)
    {
        var parametros = await _parametroRepo.ObtenerTodosAsync(request.IdEmpresa, cancellationToken);
        return parametros.Select(p => new ParametroDto(
            p.IdParametro, p.IdEmpresa, p.Clave, p.Valor, p.Descripcion)).ToList();
    }
}
