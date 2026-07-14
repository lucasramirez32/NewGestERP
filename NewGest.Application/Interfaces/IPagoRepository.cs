using NewGest.Domain.Entities.Com;

namespace NewGest.Application.Interfaces;

public interface IPagoRepository
{
    Task<Pago?> GetByIdAsync(int idPago, int idEmpresa, CancellationToken ct);
    Task<(IEnumerable<Pago> Items, int Total)> GetPagedAsync(int idEmpresa, int idCliente, int pagina, int tamano, CancellationToken ct);
    Task AddAsync(Pago pago, CancellationToken ct);
    Task<long> ObtenerProximoNumeroAsync(int idEmpresa, CancellationToken ct);
}
