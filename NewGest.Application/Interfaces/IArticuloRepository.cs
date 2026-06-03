using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Interfaces;

public interface IArticuloRepository
{
    Task<PagedResult<ArticuloListItemDto>> GetPagedAsync(
        int idEmpresa,
        string? search,
        int? idGrupo,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Articulo?> GetByIdAsync(int idEmpresa, int idArticulo, CancellationToken ct);
    Task<bool> ExisteCodigoAsync(int idEmpresa, string codigo, int? excludeId, CancellationToken ct);
    Task AddAsync(Articulo articulo, CancellationToken ct);
    void Update(Articulo articulo);
}
