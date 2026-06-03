using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.CrearMutual;

public record CrearMutualCommand(int IdEmpresa, CrearMutualDto Datos) : IRequest<MutualDto>;
