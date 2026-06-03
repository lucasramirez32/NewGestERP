using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.ActualizarEmpleado;

public record ActualizarEmpleadoCommand(int IdEmpresa, int IdEmpleado, ActualizarEmpleadoDto Datos) : IRequest<EmpleadoDto>;
