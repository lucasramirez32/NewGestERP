using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class GrupoArticuloRepository : IGrupoArticuloRepository
{
    private readonly NewgestDbContext _db;

    public GrupoArticuloRepository(NewgestDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GrupoArticuloDto>> GetArbolAsync(int idEmpresa, CancellationToken ct)
    {
        var todos = await _db.GruposArticulos
            .Where(g => g.IdEmpresa == idEmpresa)
            .OrderBy(g => g.Descripcion)
            .ToListAsync(ct);

        // Construir árbol recursivo desde los nodos raíz (sin padre)
        return BuildTree(todos, null);
    }

    private static IReadOnlyList<GrupoArticuloDto> BuildTree(
        List<GrupoArticulo> todos,
        int? idPadre)
    {
        return todos
            .Where(g => g.IdGrupoPadre == idPadre)
            .Select(g => new GrupoArticuloDto(
                g.IdGrupo,
                g.Descripcion,
                g.IdGrupoPadre,
                BuildTree(todos, g.IdGrupo)))
            .ToList();
    }

    public Task<GrupoArticulo?> GetByIdAsync(int idEmpresa, int idGrupo, CancellationToken ct)
        => _db.GruposArticulos
            .FirstOrDefaultAsync(g => g.IdEmpresa == idEmpresa && g.IdGrupo == idGrupo, ct);

    public Task<GrupoArticulo?> GetByDescripcionAsync(int idEmpresa, string descripcion, CancellationToken ct)
    {
        var descUpper = descripcion.Trim().ToUpper();
        return _db.GruposArticulos
            .FirstOrDefaultAsync(g => g.IdEmpresa == idEmpresa && g.Descripcion.Trim().ToUpper() == descUpper, ct);
    }

    public async Task AddAsync(GrupoArticulo grupo, CancellationToken ct)
        => await _db.GruposArticulos.AddAsync(grupo, ct);
}
