using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class ViajeRepository : IViajeRepository
{
    private readonly NewgestDbContext _db;

    public ViajeRepository(NewgestDbContext db) => _db = db;

    public async Task<IReadOnlyList<Viaje>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct)
        => await _db.Viajes
            .Where(v => v.IdEmpresa == idEmpresa)
            .OrderByDescending(v => v.FechaViaje)
            .ToListAsync(ct);

    public Task<Viaje?> GetByIdAsync(int idEmpresa, int idViaje, CancellationToken ct)
        => _db.Viajes.FirstOrDefaultAsync(
            v => v.IdEmpresa == idEmpresa && v.IdViaje == idViaje, ct);

    public async Task AddAsync(Viaje viaje, CancellationToken ct)
        => await _db.Viajes.AddAsync(viaje, ct);

    public void Update(Viaje viaje)
        => _db.Viajes.Update(viaje);
}
