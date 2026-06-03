using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Interfaces;

public interface IEmpleadoRepository
{
    Task<PagedResult<EmpleadoListItemDto>> GetPagedAsync(int idEmpresa, string? search, bool? soloVendedores, int page, int pageSize, CancellationToken ct);
    Task<Empleado?> GetByIdAsync(int idEmpresa, int idEmpleado, CancellationToken ct);
    Task<bool> ExisteLegajoAsync(int idEmpresa, string legajo, int? excludeId, CancellationToken ct);
    Task AddAsync(Empleado empleado, CancellationToken ct);
    void Update(Empleado empleado);
}
