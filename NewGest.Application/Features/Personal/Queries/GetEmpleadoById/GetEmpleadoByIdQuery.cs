using MediatR;
using NewGest.Application.DTOs.Personal;

namespace NewGest.Application.Features.Personal.Queries.GetEmpleadoById;

public record GetEmpleadoByIdQuery(int IdEmpresa, int IdEmpleado) : IRequest<EmpleadoDto?>;
