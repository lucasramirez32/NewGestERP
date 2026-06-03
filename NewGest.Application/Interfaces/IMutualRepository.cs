using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Interfaces;

public interface IMutualRepository
{
    Task<IReadOnlyList<Mutual>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct);
    Task<Mutual?> GetByIdAsync(int idEmpresa, int idMutual, CancellationToken ct);
    Task AddAsync(Mutual mutual, CancellationToken ct);
    void Update(Mutual mutual);
}
