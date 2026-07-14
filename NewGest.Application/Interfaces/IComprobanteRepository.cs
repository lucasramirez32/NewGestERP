using NewGest.Domain.Entities.Com;

namespace NewGest.Application.Interfaces;

public interface IComprobanteRepository
{
    Task<Comprobante?> GetByIdAsync(int idComprobante, int idEmpresa, CancellationToken ct);
    Task<(IEnumerable<Comprobante> Items, int Total)> GetPagedAsync(
        int idEmpresa, int pagina, int tamano, CancellationToken ct);
    Task AddAsync(Comprobante comprobante, CancellationToken ct);
    Task<long> ObtenerProximoNumeroAsync(int idEmpresa, int puntoVenta, Domain.Enums.TipoComprobante tipo, CancellationToken ct);
}
