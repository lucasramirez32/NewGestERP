using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly NewgestDbContext _db;

    public EmpleadoRepository(NewgestDbContext db) => _db = db;

    public async Task<PagedResult<EmpleadoListItemDto>> GetPagedAsync(
        int idEmpresa,
        string? search,
        bool? soloVendedores,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.Empleados.Where(e => e.IdEmpresa == idEmpresa);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            query = query.Where(e =>
                e.Legajo.Contains(s) ||
                e.ApellidoNombre.Contains(search) ||
                (e.CUIL != null && e.CUIL.Contains(s)));
        }

        if (soloVendedores == true)
            query = query.Where(e => e.EsVendedor);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.ApellidoNombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmpleadoListItemDto(
                e.IdEmpleado,
                e.Legajo.Trim(),
                e.ApellidoNombre,
                e.CUIL,
                e.Rol,
                e.Rol.ToString(),
                e.EsVendedor,
                e.ComisionPorcentaje,
                e.Activo))
            .ToListAsync(ct);

        return new PagedResult<EmpleadoListItemDto>(items, total, page, pageSize);
    }

    public Task<Empleado?> GetByIdAsync(int idEmpresa, int idEmpleado, CancellationToken ct)
        => _db.Empleados.FirstOrDefaultAsync(
            e => e.IdEmpresa == idEmpresa && e.IdEmpleado == idEmpleado, ct);

    public Task<bool> ExisteLegajoAsync(int idEmpresa, string legajo, int? excludeId, CancellationToken ct)
    {
        var legajoUpper = legajo.Trim().ToUpper();
        return _db.Empleados
            .Where(e => e.IdEmpresa == idEmpresa && e.Legajo.Trim() == legajoUpper)
            .Where(e => excludeId == null || e.IdEmpleado != excludeId)
            .AnyAsync(ct);
    }

    public async Task AddAsync(Empleado empleado, CancellationToken ct)
        => await _db.Empleados.AddAsync(empleado, ct);

    public void Update(Empleado empleado)
        => _db.Empleados.Update(empleado);
}
