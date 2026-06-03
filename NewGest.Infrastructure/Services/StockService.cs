using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.DTOs.Stock;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Inv;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly NewgestDbContext _db;

    public StockService(NewgestDbContext db) => _db = db;

    /// <summary>
    /// Registra el movimiento y actualiza la existencia correspondiente.
    /// La actualización de Cantidad se realiza con MERGE para garantizar
    /// atomicidad bajo concurrencia (evita lost-update — reemplaza el RLOCK() de VFP).
    /// NO abre su propia transacción — el caller maneja el UoW.
    /// </summary>
    public async Task RegistrarMovimientoAsync(MovimientoStock mov, CancellationToken ct)
    {
        await _db.MovimientosStock.AddAsync(mov, ct);

        if (mov.Tipo == TipoMovimiento.Ajuste)
        {
            // Ajuste: fijar la cantidad al valor absoluto.
            // Usa MERGE para crear la fila si no existe, o actualizar si ya existe.
            await _db.Database.ExecuteSqlRawAsync(
                """
                MERGE inv.ExistenciasDeposito WITH (HOLDLOCK) AS tgt
                USING (SELECT {0} AS IdEmpresa, {1} AS IdArticulo, {2} AS IdDeposito) AS src
                  ON tgt.IdEmpresa = src.IdEmpresa
                 AND tgt.IdArticulo = src.IdArticulo
                 AND tgt.IdDeposito = src.IdDeposito
                WHEN MATCHED THEN
                    UPDATE SET Cantidad = {3}
                WHEN NOT MATCHED THEN
                    INSERT (IdEmpresa, IdArticulo, IdDeposito, Cantidad, CostoPromedio, StockMinimo)
                    VALUES ({0}, {1}, {2}, {3}, 0, 0);
                """,
                mov.IdEmpresa, mov.IdArticulo, mov.IdDeposito, mov.Cantidad);
        }
        else
        {
            // Entrada/Salida/Transferencia: delta incremental — MERGE con suma atómica.
            var delta = mov.Tipo switch
            {
                TipoMovimiento.Entrada => mov.Cantidad,
                TipoMovimiento.Salida => -mov.Cantidad,
                TipoMovimiento.Transferencia => -mov.Cantidad,   // origen; destino en otro mov
                _ => throw new DomainException($"Tipo de movimiento desconocido: {mov.Tipo}")
            };

            await _db.Database.ExecuteSqlRawAsync(
                """
                MERGE inv.ExistenciasDeposito WITH (HOLDLOCK) AS tgt
                USING (SELECT {0} AS IdEmpresa, {1} AS IdArticulo, {2} AS IdDeposito) AS src
                  ON tgt.IdEmpresa = src.IdEmpresa
                 AND tgt.IdArticulo = src.IdArticulo
                 AND tgt.IdDeposito = src.IdDeposito
                WHEN MATCHED THEN
                    UPDATE SET Cantidad = tgt.Cantidad + {3}
                WHEN NOT MATCHED THEN
                    INSERT (IdEmpresa, IdArticulo, IdDeposito, Cantidad, CostoPromedio, StockMinimo)
                    VALUES ({0}, {1}, {2}, {3}, 0, 0);
                """,
                mov.IdEmpresa, mov.IdArticulo, mov.IdDeposito, delta);

            // Actualizar costo promedio ponderado en entradas — requiere re-leer la fila
            if (mov.Tipo == TipoMovimiento.Entrada && mov.CostoUnitario > 0)
            {
                await _db.Database.ExecuteSqlRawAsync(
                    """
                    UPDATE inv.ExistenciasDeposito
                    SET CostoPromedio =
                        CASE
                            WHEN Cantidad <= 0 THEN {3}
                            WHEN (Cantidad - {2}) <= 0 THEN {3}
                            ELSE (((Cantidad - {2}) * CostoPromedio) + ({2} * {3})) / Cantidad
                        END
                    WHERE IdEmpresa = {0} AND IdArticulo = {1} AND IdDeposito = {4}
                    """,
                    mov.IdEmpresa, mov.IdArticulo, mov.Cantidad, mov.CostoUnitario, mov.IdDeposito);
            }
        }
    }

    public async Task<IReadOnlyList<ExistenciaDepositoDto>> GetExistenciasAsync(
        int idEmpresa, int idArticulo, CancellationToken ct)
    {
        return await _db.ExistenciasDeposito
            .Where(e => e.IdEmpresa == idEmpresa && e.IdArticulo == idArticulo)
            .Join(_db.Depositos.IgnoreQueryFilters(),
                e => e.IdDeposito, d => d.IdDeposito,
                (e, d) => new { e, d })
            .Join(_db.Articulos.IgnoreQueryFilters(),
                ed => ed.e.IdArticulo, a => a.IdArticulo,
                (ed, a) => new ExistenciaDepositoDto(
                    ed.e.IdExistencia,
                    ed.e.IdArticulo,
                    a.Codigo.Trim(),
                    a.Descripcion,
                    ed.e.IdDeposito,
                    ed.d.Descripcion,
                    ed.e.Cantidad,
                    ed.e.CostoPromedio,
                    ed.e.StockMinimo,
                    CalcularNivel(ed.e.Cantidad, ed.e.StockMinimo)))
            .ToListAsync(ct);
    }

    public async Task<PagedResult<MovimientoStockDto>> GetHistorialAsync(
        int idEmpresa, int idArticulo, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.MovimientosStock
            .Where(m => m.IdEmpresa == idEmpresa && m.IdArticulo == idArticulo);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(m => m.FechaMovimiento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(_db.Articulos.IgnoreQueryFilters(),
                m => m.IdArticulo, a => a.IdArticulo,
                (m, a) => new { m, a })
            .Join(_db.Depositos.IgnoreQueryFilters(),
                ma => ma.m.IdDeposito, d => d.IdDeposito,
                (ma, d) => new MovimientoStockDto(
                    ma.m.IdMovimiento,
                    ma.m.IdArticulo,
                    ma.a.Descripcion,
                    ma.m.IdDeposito,
                    d.Descripcion,
                    ma.m.Tipo,
                    ma.m.Tipo.ToString(),
                    ma.m.Cantidad,
                    ma.m.CostoUnitario,
                    ma.m.NumeroSerie,
                    ma.m.IdComprobanteOrigen,
                    ma.m.FechaMovimiento,
                    ma.m.Observaciones))
            .ToListAsync(ct);

        return new PagedResult<MovimientoStockDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<ExistenciaDepositoDto>> GetAlertasReposicionAsync(
        int idEmpresa, CancellationToken ct)
    {
        // Solo las existencias donde Cantidad <= StockMinimo y el stock mínimo > 0
        return await _db.ExistenciasDeposito
            .Where(e => e.IdEmpresa == idEmpresa && e.StockMinimo > 0 && e.Cantidad <= e.StockMinimo)
            .Join(_db.Depositos.IgnoreQueryFilters(),
                e => e.IdDeposito, d => d.IdDeposito,
                (e, d) => new { e, d })
            .Join(_db.Articulos.IgnoreQueryFilters(),
                ed => ed.e.IdArticulo, a => a.IdArticulo,
                (ed, a) => new ExistenciaDepositoDto(
                    ed.e.IdExistencia,
                    ed.e.IdArticulo,
                    a.Codigo.Trim(),
                    a.Descripcion,
                    ed.e.IdDeposito,
                    ed.d.Descripcion,
                    ed.e.Cantidad,
                    ed.e.CostoPromedio,
                    ed.e.StockMinimo,
                    CalcularNivel(ed.e.Cantidad, ed.e.StockMinimo)))
            .ToListAsync(ct);
    }

    public async Task DescontarStockAsync(int idEmpresa, int idArticulo, decimal cantidad, CancellationToken ct)
    {
        // Descuenta del primer depósito activo de la empresa — en Sprint 9+ se podrá especificar depósito
        var deposito = await _db.Depositos
            .Where(d => d.IdEmpresa == idEmpresa && d.Activo)
            .OrderBy(d => d.IdDeposito)
            .FirstOrDefaultAsync(ct);

        if (deposito is null) return;

        var mov = MovimientoStock.Crear(
            idEmpresa, idArticulo, deposito.IdDeposito,
            TipoMovimiento.Salida, cantidad, 0, null, null, "Facturación");

        await RegistrarMovimientoAsync(mov, ct);
    }

    private static string CalcularNivel(decimal cantidad, decimal stockMinimo)
    {
        if (stockMinimo <= 0) return "verde";
        if (cantidad < stockMinimo) return "rojo";
        if (cantidad == stockMinimo) return "amarillo";
        return "verde";
    }
}
