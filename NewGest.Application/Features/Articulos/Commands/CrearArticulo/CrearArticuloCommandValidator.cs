using FluentValidation;

namespace NewGest.Application.Features.Articulos.Commands.CrearArticulo;

public class CrearArticuloCommandValidator : AbstractValidator<CrearArticuloCommand>
{
    private static readonly decimal[] IvaPermitidos = [0m, 10.5m, 21m];

    public CrearArticuloCommandValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es requerido.")
            .MaximumLength(15).WithMessage("El código no puede superar 15 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es requerida.")
            .MaximumLength(100).WithMessage("La descripción no puede superar 100 caracteres.");

        RuleFor(x => x.IdGrupo)
            .GreaterThan(0).WithMessage("Debe seleccionar un grupo.")
            .When(x => x.IdGrupo.HasValue);

        RuleFor(x => x.IdUnidad)
            .GreaterThan(0).WithMessage("Debe seleccionar una unidad.");

        RuleFor(x => x.PrecioLista)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de lista no puede ser negativo.");

        RuleFor(x => x.PrecioCosto)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de costo no puede ser negativo.");

        RuleFor(x => x.PorcentajeIva)
            .Must(iva => IvaPermitidos.Contains(iva))
            .WithMessage("El porcentaje de IVA debe ser 0, 10.5 o 21.");
    }
}
