using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NewGest.Application.Common;
using NewGest.Application.DTOs;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Users;

namespace NewGest.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private const int MaxIntentosFallidos = 5;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        ILogger<LoginCommandHandler> logger)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Buscar usuario por empresa + nombre de usuario
        var usuario = await _userManager.FindByNameAsync(request.NombreUsuario);

        // Si el usuario no pertenece a la empresa indicada, tratarlo como no encontrado
        if (usuario is null || usuario.IdEmpresa != request.IdEmpresa)
        {
            _logger.LogWarning("Intento de login fallido para usuario {NombreUsuario} en empresa {IdEmpresa}: no encontrado",
                request.NombreUsuario, request.IdEmpresa);
            throw new DomainException("Credenciales incorrectas.");
        }

        if (usuario.Bloqueado)
        {
            _logger.LogWarning("Intento de login en cuenta bloqueada: {NombreUsuario}", request.NombreUsuario);
            throw new DomainException("La cuenta está bloqueada por demasiados intentos fallidos. Contacte al administrador.");
        }

        var passwordValido = await _userManager.CheckPasswordAsync(usuario, request.Password);
        if (!passwordValido)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentosFallidos)
            {
                usuario.Bloqueado = true;
                _logger.LogWarning("Cuenta bloqueada por intentos fallidos: {NombreUsuario}", request.NombreUsuario);
            }
            await _userManager.UpdateAsync(usuario);
            throw new DomainException("Credenciales incorrectas.");
        }

        // Login correcto
        usuario.IntentosFallidos = 0;
        usuario.UltimoLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(usuario);

        var claims = await _userManager.GetClaimsAsync(usuario);
        var token = _jwtService.GenerarToken(usuario, claims);

        _logger.LogInformation("Login exitoso: usuario {NombreUsuario} empresa {IdEmpresa}", request.NombreUsuario, request.IdEmpresa);

        return new LoginResultDto(
            Token: token,
            IdUsuario: usuario.Id,
            Nombre: usuario.Nombre,
            IdEmpresa: usuario.IdEmpresa,
            DebeResetearPassword: usuario.DebeResetearPassword);
    }
}
