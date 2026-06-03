using NewGest.Domain.Entities.Empresas;

namespace NewGest.Application.Interfaces;

public interface IEmpresaRepository
{
    Task<IReadOnlyList<Empresa>> ObtenerActivasAsync(CancellationToken cancellationToken = default);
    Task<Empresa?> ObtenerPorIdAsync(int idEmpresa, CancellationToken cancellationToken = default);
}
