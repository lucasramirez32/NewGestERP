using MediatR;
using NewGest.Application.DTOs.Cobranzas;

namespace NewGest.Application.Features.Cobranzas.Commands.CrearPago;

public record CrearPagoCommand(int IdEmpresa, CrearPagoDto Datos) : IRequest<PagoDto>;
