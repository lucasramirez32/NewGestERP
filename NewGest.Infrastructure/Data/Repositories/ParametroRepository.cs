using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Config;

namespace NewGest.Infrastructure.Data.Repositories;

public class ParametroRepository : IParametroRepository
{
    private readonly NewgestDbContext _db;

    public ParametroRepository(NewgestDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Parametro>> ObtenerTodosAsync(int? idEmpresa, CancellationToken cancellationToken = default)
        => await _db.Parametros
            .Where(p => p.IdEmpresa == null || p.IdEmpresa == idEmpresa)
            .OrderBy(p => p.Clave)
            .ToListAsync(cancellationToken);

    public async Task<Parametro?> ObtenerPorClaveAsync(string clave, int? idEmpresa, CancellationToken cancellationToken = default)
        => await _db.Parametros
            .FirstOrDefaultAsync(p => p.Clave == clave && p.IdEmpresa == idEmpresa, cancellationToken);

    public async Task ActualizarValorAsync(string clave, int? idEmpresa, string nuevoValor, CancellationToken cancellationToken = default)
    {
        var parametro = await ObtenerPorClaveAsync(clave, idEmpresa, cancellationToken)
            ?? throw new DomainException($"Parámetro '{clave}' no encontrado.");

        parametro.Valor = nuevoValor;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
