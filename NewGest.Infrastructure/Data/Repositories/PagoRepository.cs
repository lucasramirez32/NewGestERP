using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;

namespace NewGest.Infrastructure.Data.Repositories;

public class PagoRepository : IPagoRepository
{
    private readonly NewgestDbContext _db;

    public PagoRepository(NewgestDbContext db) => _db = db;

    public async Task<Pago?> GetByIdAsync(int idPago, int idEmpresa, CancellationToken ct) =>
        await _db.Pagos
            .Include(p => p.Medios)
            .Include(p => p.Imputaciones)
            .Include(p => p.Retenciones)
            .FirstOrDefaultAsync(p => p.IdPago == idPago && p.IdEmpresa == idEmpresa, ct);

    public async Task<(IEnumerable<Pago> Items, int Total)> GetPagedAsync(
        int idEmpresa, int idCliente, int pagina, int tamano, CancellationToken ct)
    {
        var query = _db.Pagos
            .Include(p => p.Medios)
            .Include(p => p.Imputaciones)
            .Where(p => p.IdEmpresa == idEmpresa && p.IdCliente == idCliente && !p.Anulado)
            .OrderByDescending(p => p.Numero);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(Pago pago, CancellationToken ct) =>
        await _db.Pagos.AddAsync(pago, ct);

    public async Task<long> ObtenerProximoNumeroAsync(int idEmpresa, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync(
            """
            MERGE com.NumeradoresPago WITH (HOLDLOCK) AS tgt
            USING (SELECT {0} AS IdEmpresa) AS src ON tgt.IdEmpresa = src.IdEmpresa
            WHEN NOT MATCHED THEN INSERT (IdEmpresa, UltimoNumero) VALUES ({0}, 0);
            """,
            new object[] { idEmpresa },
            ct);

        var result = await _db.Database.SqlQueryRaw<long>(
            """
            UPDATE com.NumeradoresPago WITH (ROWLOCK)
            SET UltimoNumero = UltimoNumero + 1
            OUTPUT INSERTED.UltimoNumero
            WHERE IdEmpresa = {0}
            """, idEmpresa).ToListAsync(ct);

        if (result.Count == 0)
            throw new DomainException("No se pudo reservar número de recibo.");

        return result[0];
    }
}
