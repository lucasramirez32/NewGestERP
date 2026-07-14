using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Cnt;

namespace NewGest.Infrastructure.Data.Repositories;

public class CuentaContableRepository : ICuentaContableRepository
{
    private readonly NewgestDbContext _db;

    public CuentaContableRepository(NewgestDbContext db) => _db = db;

    public async Task<CuentaContable?> GetByIdAsync(int idCuenta, int idEmpresa, CancellationToken ct) =>
        await _db.CuentasContables.FirstOrDefaultAsync(
            c => c.IdCuenta == idCuenta && c.IdEmpresa == idEmpresa, ct);

    public async Task<CuentaContable?> GetByCodigoAsync(int idEmpresa, string codigo, CancellationToken ct) =>
        await _db.CuentasContables.FirstOrDefaultAsync(
            c => c.IdEmpresa == idEmpresa && c.Codigo == codigo, ct);

    public async Task<IReadOnlyList<CuentaContable>> GetAllAsync(int idEmpresa, CancellationToken ct) =>
        await _db.CuentasContables
            .Where(c => c.IdEmpresa == idEmpresa)
            .OrderBy(c => c.Codigo)
            .ToListAsync(ct);

    public async Task AddAsync(CuentaContable cuenta, CancellationToken ct) =>
        await _db.CuentasContables.AddAsync(cuenta, ct);
}
