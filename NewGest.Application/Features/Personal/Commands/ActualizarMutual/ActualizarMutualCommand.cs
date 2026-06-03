using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.ActualizarMutual;

public record ActualizarMutualCommand(int IdEmpresa, int IdMutual, ActualizarMutualDto Datos) : IRequest<MutualDto>;
