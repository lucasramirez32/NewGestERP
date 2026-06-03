using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.ActualizarViaje;

public record ActualizarViajeCommand(int IdEmpresa, int IdViaje, ActualizarViajeDto Datos) : IRequest<ViajeDto>;
