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

        RuleFor(x => x.NombreFantasia)
            .MaximumLength(100).WithMessage("El nombre de fantasía no puede superar 100 caracteres.");

        RuleFor(x => x.LimiteCredito)
            .GreaterThanOrEqualTo(0).WithMessage("El límite de crédito debe ser mayor o igual a 0.");

        RuleFor(x => x.DiasMora)
            .GreaterThanOrEqualTo(0).WithMessage("Los días de mora deben ser mayores o igual a 0.");

        RuleFor(x => x.Descuento)
            .InclusiveBetween(0, 100).WithMessage("El descuento debe estar entre 0 y 100.");

        RuleFor(x => x.Provincia)
            .MaximumLength(50).WithMessage("La provincia no puede superar 50 caracteres.");

        RuleFor(x => x.CodigoPostal)
            .MaximumLength(10).WithMessage("El código postal no puede superar 10 caracteres.");

        RuleFor(x => x.ObraSocial)
            .MaximumLength(100).WithMessage("La obra social no puede superar 100 caracteres.");

        RuleFor(x => x.NroAfiliado)
            .MaximumLength(50).WithMessage("El número de afiliado no puede superar 50 caracteres.");

        RuleFor(x => x.MedicoCabecera)
            .MaximumLength(100).WithMessage("El médico de cabecera no puede superar 100 caracteres.");

        RuleFor(x => x.MatriculaMedico)
            .MaximumLength(30).WithMessage("La matrícula del médico no puede superar 30 caracteres.");

        RuleFor(x => x.Alergias)
            .MaximumLength(200).WithMessage("Las alergias no pueden superar 200 caracteres.");

        RuleFor(x => x.Medicacion)
            .MaximumLength(200).WithMessage("La medicación no puede superar 200 caracteres.");

        RuleFor(x => x.Patologia)
            .MaximumLength(200).WithMessage("La patología no puede superar 200 caracteres.");
    }
}
