using NewGest.Application.DTOs.Articulos;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Interfaces;

public interface IGrupoArticuloRepository
{
    Task<IReadOnlyList<GrupoArticuloDto>> GetArbolAsync(int idEmpresa, CancellationToken ct);
    Task<GrupoArticulo?> GetByIdAsync(int idEmpresa, int idGrupo, CancellationToken ct);
    Task<GrupoArticulo?> GetByDescripcionAsync(int idEmpresa, string descripcion, CancellationToken ct);
    Task AddAsync(GrupoArticulo grupo, CancellationToken ct);
}
