using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Domain.Enums;

namespace NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;

public record EmitirFacturaCommand(
    int IdEmpresa,
    string CuitEmisor,
    TipoComprobante Tipo,
    int PuntoVenta,
    DateOnly Fecha,
    int IdCliente,
    int? IdPedidoOrigen,
    List<ItemFacturaDto> Items
) : IRequest<FacturaEmitidaDto>;
