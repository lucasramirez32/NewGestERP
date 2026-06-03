using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Commands.CrearEmpleado;

public record CrearEmpleadoCommand(int IdEmpresa, CrearEmpleadoDto Datos) : IRequest<EmpleadoDto>;
