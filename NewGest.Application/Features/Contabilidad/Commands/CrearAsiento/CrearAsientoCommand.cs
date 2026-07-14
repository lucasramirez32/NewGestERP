using MediatR;
using NewGest.Application.DTOs.Contabilidad;

namespace NewGest.Application.Features.Contabilidad.Commands.CrearAsiento;

public record CrearAsientoCommand(int IdEmpresa, CrearAsientoDto Datos) : IRequest<AsientoDto>;
