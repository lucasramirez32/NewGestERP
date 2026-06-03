using MediatR;

namespace NewGest.Application.Features.Parametros.Commands;

public record ActualizarParametroCommand(string Clave, int? IdEmpresa, string NuevoValor)
    : IRequest<Unit>;
