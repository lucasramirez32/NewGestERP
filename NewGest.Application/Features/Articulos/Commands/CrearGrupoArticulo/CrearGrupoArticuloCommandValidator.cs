using FluentValidation;

namespace NewGest.Application.Features.Articulos.Commands.CrearGrupoArticulo;

public class CrearGrupoArticuloCommandValidator : AbstractValidator<CrearGrupoArticuloCommand>
{
    public CrearGrupoArticuloCommandValidator()
    {
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción del grupo es requerida.")
            .MaximumLength(100).WithMessage("La descripción no puede superar 100 caracteres.");
    }
}
