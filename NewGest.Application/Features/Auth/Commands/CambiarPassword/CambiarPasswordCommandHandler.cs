using MediatR;
using Microsoft.AspNetCore.Identity;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Users;

namespace NewGest.Application.Features.Auth.Commands.CambiarPassword;

public class CambiarPasswordCommandHandler : IRequestHandler<CambiarPasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CambiarPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Unit> Handle(CambiarPasswordCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(request.IdUsuario.ToString())
            ?? throw new DomainException("Usuario no encontrado.");

        var resultado = await _userManager.ChangePasswordAsync(
            usuario,
            request.PasswordActual,
            request.PasswordNueva);

        if (!resultado.Succeeded)
        {
            var errores = string.Join(", ", resultado.Errors.Select(e => e.Description));
            throw new DomainException($"No se pudo cambiar la contraseña: {errores}");
        }

        // Marcar que ya no necesita resetear
        usuario.DebeResetearPassword = false;
        await _userManager.UpdateAsync(usuario);

        return Unit.Value;
    }
}
