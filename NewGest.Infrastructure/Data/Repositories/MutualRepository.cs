using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class MutualRepository : IMutualRepository
{
    private readonly NewgestDbContext _db;

    public MutualRepository(NewgestDbContext db) => _db = db;

    public async Task<IReadOnlyList<Mutual>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct)
        => await _db.Mutuales
            .Where(m => m.IdEmpresa == idEmpresa)
            .OrderBy(m => m.Descripcion)
            .ToListAsync(ct);

    public Task<Mutual?> GetByIdAsync(int idEmpresa, int idMutual, CancellationToken ct)
        => _db.Mutuales.FirstOrDefaultAsync(
            m => m.IdEmpresa == idEmpresa && m.IdMutual == idMutual, ct);

    public async Task AddAsync(Mutual mutual, CancellationToken ct)
        => await _db.Mutuales.AddAsync(mutual, ct);

    public void Update(Mutual mutual)
        => _db.Mutuales.Update(mutual);
}
