using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Empresas;

namespace NewGest.Infrastructure.Data.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly NewgestDbContext _db;

    public EmpresaRepository(NewgestDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Empresa>> ObtenerActivasAsync(CancellationToken cancellationToken = default)
        => await _db.Empresas
            .Where(e => e.Activa)
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);

    public async Task<Empresa?> ObtenerPorIdAsync(int idEmpresa, CancellationToken cancellationToken = default)
        => await _db.Empresas.FindAsync([idEmpresa], cancellationToken);
}
