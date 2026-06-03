using MediatR;
using NewGest.Application.DTOs;

namespace NewGest.Application.Features.Auth.Commands.Login;

public record LoginCommand(int IdEmpresa, string NombreUsuario, string Password)
    : IRequest<LoginResultDto>;
