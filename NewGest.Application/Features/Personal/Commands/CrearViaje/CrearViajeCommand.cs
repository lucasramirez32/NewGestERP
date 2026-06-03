using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.CrearViaje;

public record CrearViajeCommand(int IdEmpresa, CrearViajeDto Datos) : IRequest<ViajeDto>;
