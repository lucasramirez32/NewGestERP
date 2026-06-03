using NewGest.Domain.Entities.Neg;

namespace NewGest.Application.Interfaces;

public interface IViajeRepository
{
    Task<IReadOnlyList<Viaje>> GetByEmpresaAsync(int idEmpresa, CancellationToken ct);
    Task<Viaje?> GetByIdAsync(int idEmpresa, int idViaje, CancellationToken ct);
    Task AddAsync(Viaje viaje, CancellationToken ct);
    void Update(Viaje viaje);
}
