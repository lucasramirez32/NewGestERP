using MediatR;
using NewGest.Application.DTOs;

namespace NewGest.Application.Features.Parametros.Queries;

public record GetParametrosQuery(int? IdEmpresa = null) : IRequest<IReadOnlyList<ParametroDto>>;
