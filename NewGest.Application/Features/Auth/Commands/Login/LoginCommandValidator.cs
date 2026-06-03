using FluentValidation;

namespace NewGest.Application.Features.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.IdEmpresa).GreaterThan(0).WithMessage("Debe seleccionar una empresa.");
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("El nombre de usuario es requerido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
    }
}
