using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;

namespace NewGest.Infrastructure.Data.Repositories;

public class AsientoRepository : IAsientoRepository
{
    private readonly NewgestDbContext _db;

    public AsientoRepository(NewgestDbContext db) => _db = db;

    public async Task<Asiento?> GetByIdAsync(int idAsiento, int idEmpresa, CancellationToken ct) =>
        await _db.Asientos
            .Include(a => a.Partidas)
            .FirstOrDefaultAsync(a => a.IdAsiento == idAsiento && a.IdEmpresa == idEmpresa, ct);

    public async Task<(IEnumerable<Asiento> Items, int Total)> GetPagedAsync(
        int idEmpresa, int pagina, int tamano, CancellationToken ct)
    {
        var query = _db.Asientos
            .Include(a => a.Partidas)
            .Where(a => a.IdEmpresa == idEmpresa && !a.Anulado)
            .OrderByDescending(a => a.Numero);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Asiento asiento, CancellationToken ct) =>
        await _db.Asientos.AddAsync(asiento, ct);

    /// <summary>
    /// UPDATE atómico con ROWLOCK sobre NumeradoresAsiento — mismo patrón que comprobantes.
    /// </summary>
    public async Task<long> ObtenerProximoNumeroAsync(int idEmpresa, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync(
            """
            MERGE cnt.NumeradoresAsiento WITH (HOLDLOCK) AS tgt
            USING (SELECT {0} AS IdEmpresa) AS src ON tgt.IdEmpresa = src.IdEmpresa
            WHEN NOT MATCHED THEN INSERT (IdEmpresa, UltimoNumero) VALUES ({0}, 0);
            """, idEmpresa, ct);

        var resultado = await _db.Database.SqlQueryRaw<long>(
            """
            UPDATE cnt.NumeradoresAsiento WITH (ROWLOCK)
            SET UltimoNumero = UltimoNumero + 1
            OUTPUT INSERTED.UltimoNumero
            WHERE IdEmpresa = {0}
            """, idEmpresa).ToListAsync(ct);

        if (resultado.Count == 0)
            throw new DomainException("No se pudo reservar número de asiento.");

        return resultado[0];
    }
}
