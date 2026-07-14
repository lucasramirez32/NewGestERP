using MediatR;
using NewGest.Application.DTOs.Contabilidad;

namespace NewGest.Application.Features.Contabilidad.Queries.GetPlanCuentas;

public record GetPlanCuentasQuery(int IdEmpresa) : IRequest<IReadOnlyList<CuentaContableDto>>;
