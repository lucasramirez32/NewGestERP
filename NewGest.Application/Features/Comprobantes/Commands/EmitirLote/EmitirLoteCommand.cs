using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirLote;

public record EmitirLoteCommand(
    int IdEmpresa,
    string CuitEmisor,
    List<EmitirFacturaCommand> Comprobantes
) : IRequest<LoteResultDto>;
