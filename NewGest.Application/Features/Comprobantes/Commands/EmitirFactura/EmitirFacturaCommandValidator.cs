using FluentValidation;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

public class EmitirFacturaCommandValidator : AbstractValidator<EmitirFacturaCommand>
{
    public EmitirFacturaCommandValidator()
    {
        RuleFor(x => x.PuntoVenta).InclusiveBetween(1, 9999);
        RuleFor(x => x.Fecha).NotEmpty();
        RuleFor(x => x.IdCliente).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("El comprobante debe tener al menos un ítem.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.IdArticulo).GreaterThan(0);
            item.RuleFor(i => i.Cantidad).GreaterThan(0);
            item.RuleFor(i => i.PrecioUnitario).GreaterThanOrEqualTo(0);
        });
    }
}
