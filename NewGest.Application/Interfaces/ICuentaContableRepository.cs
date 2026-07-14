using NewGest.Domain.Entities.Cnt;

namespace NewGest.Application.Interfaces;

public interface ICuentaContableRepository
{
    Task<CuentaContable?> GetByIdAsync(int idCuenta, int idEmpresa, CancellationToken ct);
    Task<CuentaContable?> GetByCodigoAsync(int idEmpresa, string codigo, CancellationToken ct);
    Task<IReadOnlyList<CuentaContable>> GetAllAsync(int idEmpresa, CancellationToken ct);
    Task AddAsync(CuentaContable cuenta, CancellationToken ct);
}
