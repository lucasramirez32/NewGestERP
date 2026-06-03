using FluentValidation;

namespace NewGest.Application.Features.Auth.Commands.CambiarPassword;

public class CambiarPasswordCommandValidator : AbstractValidator<CambiarPasswordCommand>
{
    public CambiarPasswordCommandValidator()
    {
        RuleFor(x => x.IdUsuario).GreaterThan(0);
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("La contraseña actual es requerida.");
        RuleFor(x => x.PasswordNueva)
            .NotEmpty().WithMessage("La nueva contraseña es requerida.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"\d").WithMessage("La contraseña debe contener al menos un número.");
    }
}
