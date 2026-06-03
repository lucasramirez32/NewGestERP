using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Inv;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class DepositoRepository : IDepositoRepository
{
    private readonly NewgestDbContext _db;

    public DepositoRepository(NewgestDbContext db) => _db = db;

    public async Task<IReadOnlyList<Deposito>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct)
        => await _db.Depositos
            .Where(d => d.IdEmpresa == idEmpresa)
            .OrderBy(d => d.Descripcion)
            .ToListAsync(ct);

    public Task<Deposito?> GetByIdAsync(int idEmpresa, int idDeposito, CancellationToken ct)
        => _db.Depositos.FirstOrDefaultAsync(
            d => d.IdEmpresa == idEmpresa && d.IdDeposito == idDeposito, ct);

    public async Task AddAsync(Deposito deposito, CancellationToken ct)
        => await _db.Depositos.AddAsync(deposito, ct);
}
