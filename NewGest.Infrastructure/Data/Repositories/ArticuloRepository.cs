using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class ArticuloRepository : IArticuloRepository
{
    private readonly NewgestDbContext _db;

    public ArticuloRepository(NewgestDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ArticuloListItemDto>> GetPagedAsync(
        int idEmpresa,
        string? search,
        int? idGrupo,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.Articulos
            .Include(a => a.Grupo)
            .Include(a => a.Unidad)
            .Where(a => a.IdEmpresa == idEmpresa);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            query = query.Where(a =>
                a.Codigo.Contains(s) ||
                a.Descripcion.Contains(search));
        }

        if (idGrupo.HasValue)
            query = query.Where(a => a.IdGrupo == idGrupo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.Descripcion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArticuloListItemDto(
                a.IdArticulo,
                a.Codigo.Trim(),
                a.Descripcion,
                a.Grupo != null ? a.Grupo.Descripcion : string.Empty,
                a.Unidad != null ? a.Unidad.Simbolo : string.Empty,
                a.PrecioLista,
                a.PorcentajeIva,
                a.Activo))
            .ToListAsync(ct);

        return new PagedResult<ArticuloListItemDto>(items, total, page, pageSize);
    }

    public Task<Articulo?> GetByIdAsync(int idEmpresa, int idArticulo, CancellationToken ct)
        => _db.Articulos
            .Include(a => a.Grupo)
            .Include(a => a.Unidad)
            .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdArticulo == idArticulo, ct);

    public Task<bool> ExisteCodigoAsync(int idEmpresa, string codigo, int? excludeId, CancellationToken ct)
    {
        var codigoUpper = codigo.Trim().ToUpper();
        return _db.Articulos
            .Where(a => a.IdEmpresa == idEmpresa && a.Codigo.Trim() == codigoUpper)
            .Where(a => excludeId == null || a.IdArticulo != excludeId)
            .AnyAsync(ct);
    }

    public async Task AddAsync(Articulo articulo, CancellationToken ct)
        => await _db.Articulos.AddAsync(articulo, ct);

    public void Update(Articulo articulo)
        => _db.Articulos.Update(articulo);
}
