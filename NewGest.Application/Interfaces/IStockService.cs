using NewGest.Application.DTOs.Shared;
using NewGest.Application.DTOs.Stock;
using NewGest.Domain.Entities.Inv;

namespace NewGest.Application.Interfaces;

public interface IStockService
{
    Task RegistrarMovimientoAsync(MovimientoStock mov, CancellationToken ct);
    Task<IReadOnlyList<ExistenciaDepositoDto>> GetExistenciasAsync(int idEmpresa, int idArticulo, CancellationToken ct);
    Task<PagedResult<MovimientoStockDto>> GetHistorialAsync(int idEmpresa, int idArticulo, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<ExistenciaDepositoDto>> GetAlertasReposicionAsync(int idEmpresa, CancellationToken ct);
    Task DescontarStockAsync(int idEmpresa, int idArticulo, decimal cantidad, CancellationToken ct);
}
