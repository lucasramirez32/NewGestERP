using MediatR;
using NewGest.Application.DTOs.Contabilidad;

namespace NewGest.Application.Features.Contabilidad.Commands.CrearCuentaContable;

public record CrearCuentaContableCommand(int IdEmpresa, CrearCuentaContableDto Datos) : IRequest<CuentaContableDto>;
