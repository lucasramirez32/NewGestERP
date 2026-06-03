using FluentValidation;

namespace NewGest.Application.Features.Clientes.Commands.ActualizarCliente;

public class ActualizarClienteCommandValidator : AbstractValidator<ActualizarClienteCommand>
{
    public ActualizarClienteCommandValidator()
    {
        RuleFor(x => x.IdCliente)
            .GreaterThan(0).WithMessage("Id de cliente inválido.");

        RuleFor(x => x.RazonSocial)
            .NotEmpty().WithMessage("La razón social es requerida.")
            .MaximumLength(100).WithMessage("La razón social no puede superar 100 caracteres.");

        RuleFor(x => x.CUIT)
            .Matches(@"^\d{2}-?\d{8}-?\d$").WithMessage("El CUIT debe tener el formato XX-XXXXXXXX-X.")
            .When(x => !string.IsNullOrWhiteSpace(x.CUIT));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no es válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CondicionIva)
            .IsInEnum().WithMessage("Condición de IVA inválida.");
    }
}
