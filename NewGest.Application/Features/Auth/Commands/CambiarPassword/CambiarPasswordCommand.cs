using MediatR;

namespace NewGest.Application.Features.Auth.Commands.CambiarPassword;

public record CambiarPasswordCommand(int IdUsuario, string PasswordActual, string PasswordNueva)
    : IRequest<Unit>;
