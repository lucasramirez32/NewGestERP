using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirLote;

public class EmitirLoteCommandHandler : IRequestHandler<EmitirLoteCommand, LoteResultDto>
{
    private readonly IMediator _mediator;

    public EmitirLoteCommandHandler(IMediator mediator) => _mediator = mediator;

    public async Task<LoteResultDto> Handle(EmitirLoteCommand request, CancellationToken ct)
    {
        // AFIP admite hasta 250 comprobantes por solicitud; procesamos secuencialmente
        // para respetar la numeración consecutiva y el rollback individual.
        var resultados = new List<LoteItemResultDto>();

        for (var i = 0; i < request.Comprobantes.Count; i++)
        {
            try
            {
                var factura = await _mediator.Send(request.Comprobantes[i], ct);
                resultados.Add(new LoteItemResultDto(i, true, factura, null));
            }
            catch (Exception ex)
            {
                resultados.Add(new LoteItemResultDto(i, false, null, ex.Message));
            }
        }

        return new LoteResultDto(
            request.Comprobantes.Count,
            resultados.Count(r => r.Exito),
            resultados.Count(r => !r.Exito),
            resultados);
    }
}
