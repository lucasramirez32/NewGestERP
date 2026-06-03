using NewGest.Domain.Entities.Inv;

namespace NewGest.Application.Interfaces;

public interface IDepositoRepository
{
    Task<IReadOnlyList<Deposito>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct);
    Task<Deposito?> GetByIdAsync(int idEmpresa, int idDeposito, CancellationToken ct);
    Task AddAsync(Deposito deposito, CancellationToken ct);
}
