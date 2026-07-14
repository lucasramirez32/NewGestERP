using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
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

    /// <summary>
    /// Obtiene y reserva el próximo número de forma atómica usando UPDATE con ROWLOCK.
    /// Reemplaza el MAX+1 que producía race conditions — equivale al RLOCK()+contador de VFP
    /// pero sin la window de duplicados que tenía esa implementación.
    /// Si no existe numerador para la combinación, lo crea con número 1.
    /// </summary>
    public async Task<long> ObtenerProximoNumeroAsync(
        int idEmpresa, int puntoVenta, TipoComprobante tipo, CancellationToken ct)
    {
        // Asegurar que existe la fila del numerador (MERGE es atómico)
        await _db.Database.ExecuteSqlRawAsync(
            """
            MERGE com.NumeradoresComprobante WITH (HOLDLOCK) AS tgt
            USING (SELECT {0} AS IdEmpresa, {1} AS PuntoVenta, {2} AS Tipo) AS src
              ON tgt.IdEmpresa = src.IdEmpresa
             AND tgt.PuntoVenta = src.PuntoVenta
             AND tgt.Tipo = src.Tipo
            WHEN NOT MATCHED THEN
                INSERT (IdEmpresa, PuntoVenta, Tipo, UltimoNumero)
                VALUES ({0}, {1}, {2}, 0);
            """,
            new object[] { idEmpresa, puntoVenta, (int)tipo },
            ct);

        // UPDATE atómico con ROWLOCK: incrementa y devuelve el nuevo valor
        var resultado = await _db.Database.SqlQueryRaw<long>(
            """
            UPDATE com.NumeradoresComprobante WITH (ROWLOCK)
            SET UltimoNumero = UltimoNumero + 1
            OUTPUT INSERTED.UltimoNumero
            WHERE IdEmpresa = {0} AND PuntoVenta = {1} AND Tipo = {2}
            """,
            idEmpresa, puntoVenta, (int)tipo)
            .ToListAsync(ct);

        if (resultado.Count == 0)
            throw new DomainException($"No se pudo reservar número para tipo {tipo}, PV {puntoVenta}.");

        return resultado[0];
    }
}
