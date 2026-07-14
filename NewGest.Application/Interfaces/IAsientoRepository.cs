using NewGest.Domain.Entities.Cnt;

namespace NewGest.Application.Interfaces;

public interface IAsientoRepository
{
    Task<Asiento?> GetByIdAsync(int idAsiento, int idEmpresa, CancellationToken ct);
    Task<(IEnumerable<Asiento> Items, int Total)> GetPagedAsync(int idEmpresa, int pagina, int tamano, CancellationToken ct);
    Task AddAsync(Asiento asiento, CancellationToken ct);
    Task<long> ObtenerProximoNumeroAsync(int idEmpresa, CancellationToken ct);
}
