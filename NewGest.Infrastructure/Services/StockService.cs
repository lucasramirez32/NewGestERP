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
    /// NO abre su propia transacción — el caller maneja el UoW.
    /// </summary>
    public async Task RegistrarMovimientoAsync(MovimientoStock mov, CancellationToken ct)
    {
        await _db.MovimientosStock.AddAsync(mov, ct);

        var existencia = await _db.ExistenciasDeposito
            .FirstOrDefaultAsync(e =>
                e.IdArticulo == mov.IdArticulo &&
                e.IdDeposito == mov.IdDeposito &&
                e.IdEmpresa == mov.IdEmpresa, ct);

        if (existencia is null)
        {
            existencia = new ExistenciaDeposito
            {
                IdEmpresa = mov.IdEmpresa,
                IdArticulo = mov.IdArticulo,
                IdDeposito = mov.IdDeposito,
                Cantidad = 0,
                CostoPromedio = 0,
                StockMinimo = 0
            };
            _db.ExistenciasDeposito.Add(existencia);
        }

        var delta = mov.Tipo switch
        {
            TipoMovimiento.Entrada => mov.Cantidad,
            TipoMovimiento.Salida => -mov.Cantidad,
            TipoMovimiento.Ajuste => mov.Cantidad - existencia.Cantidad,   // ajuste absoluto
            TipoMovimiento.Transferencia => -mov.Cantidad,                 // origen; destino en otro mov
            _ => throw new DomainException($"Tipo de movimiento desconocido: {mov.Tipo}")
        };

        existencia.Cantidad += delta;

        // Costo promedio ponderado — solo en entradas con costo informado
        if (mov.Tipo == TipoMovimiento.Entrada && mov.CostoUnitario > 0 && existencia.Cantidad > 0)
        {
            var cantidadAnterior = existencia.Cantidad - mov.Cantidad;
            existencia.CostoPromedio = cantidadAnterior <= 0
                ? mov.CostoUnitario
                : ((cantidadAnterior * existencia.CostoPromedio) + (mov.Cantidad * mov.CostoUnitario))
                  / existencia.Cantidad;
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

    private static string CalcularNivel(decimal cantidad, decimal stockMinimo)
    {
        if (stockMinimo <= 0) return "verde";
        if (cantidad < stockMinimo) return "rojo";
        if (cantidad == stockMinimo) return "amarillo";
        return "verde";
    }
}
