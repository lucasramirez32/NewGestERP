using FluentValidation;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

public class EmitirFacturaCommandValidator : AbstractValidator<EmitirFacturaCommand>
{
    public EmitirFacturaCommandValidator()
    {
        RuleFor(x => x.IdEmpresa).GreaterThan(0);
        RuleFor(x => x.PuntoVenta).InclusiveBetween(1, 9999);
        RuleFor(x => x.IdCliente).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("El comprobante debe tener al menos un ítem.");

        // AFIP rechaza fechas con más de 5 días de anticipación
        RuleFor(x => x.Fecha)
            .NotEmpty()
            .Must(f => f <= DateOnly.FromDateTime(DateTime.Today.AddDays(5)))
            .WithMessage("La fecha del comprobante no puede ser mayor a 5 días en el futuro.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.IdArticulo).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Descripcion).NotEmpty().WithMessage("La descripción del ítem no puede estar vacía.");
            item.RuleFor(i => i.Cantidad).GreaterThan(0);
            item.RuleFor(i => i.PrecioUnitario).GreaterThanOrEqualTo(0);
        });

        // FC-A y NC-A/ND-A requieren CUIT del receptor — se valida en dominio,
        // pero el validator puede dar un mensaje más temprano y descriptivo.
        // La validación definitiva de condicionIva vs tipo es responsabilidad del dominio.
    }
}
