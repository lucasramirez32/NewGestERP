using MediatR;
using NewGest.Application.DTOs;

namespace NewGest.Application.Features.Auth.Queries.GetEmpresas;

/// <summary>
/// Retorna la lista de empresas activas para el dropdown de login.
/// Esta query es pública (no requiere autenticación).
/// </summary>
public record GetEmpresasQuery : IRequest<IReadOnlyList<EmpresaDto>>;
