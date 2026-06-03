using MediatR;

namespace NewGest.Application.Features.Personal.Commands.DesactivarEmpleado;

public record DesactivarEmpleadoCommand(int IdEmpresa, int IdEmpleado) : IRequest;
