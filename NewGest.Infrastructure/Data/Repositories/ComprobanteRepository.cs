using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class ComprobanteRepository : IComprobanteRepository
{
    private readonly NewgestDbContext _db;

    public ComprobanteRepository(NewgestDbContext db) => _db = db;

    public async Task<Comprobante?> GetByIdAsync(int idComprobante, int idEmpresa, CancellationToken ct) =>
        await _db.Comprobantes
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante && c.IdEmpresa == idEmpresa, ct);

    public async Task<(IEnumerable<Comprobante> Items, int Total)> GetPagedAsync(
        int idEmpresa, int pagina, int tamano, CancellationToken ct)
    {
        var query = _db.Comprobantes
            .Include(c => c.Items)
            .Where(c => c.IdEmpresa == idEmpresa)
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Numero);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Comprobante comprobante, CancellationToken ct) =>
        await _db.Comprobantes.AddAsync(comprobante, ct);

    public async Task<long> ObtenerProximoNumeroAsync(
        int idEmpresa, int puntoVenta, TipoComprobante tipo, CancellationToken ct)
    {
        var ultimo = await _db.Comprobantes
            .Where(c => c.IdEmpresa == idEmpresa
                     && c.PuntoVenta == puntoVenta
                     && c.Tipo == tipo
                     && !c.Anulado)
            .MaxAsync(c => (long?)c.Numero, ct);

        // SEQUENCE SQL es la opción óptima, pero mientras se configura se usa MAX+1 con
        // la unicidad del índice como respaldo ante concurrencia.
        return (ultimo ?? 0) + 1;
    }
}
