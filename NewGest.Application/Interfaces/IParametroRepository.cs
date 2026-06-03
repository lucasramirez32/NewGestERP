using NewGest.Domain.Entities.Config;

namespace NewGest.Application.Interfaces;

public interface IParametroRepository
{
    Task<IReadOnlyList<Parametro>> ObtenerTodosAsync(int? idEmpresa, CancellationToken cancellationToken = default);
    Task<Parametro?> ObtenerPorClaveAsync(string clave, int? idEmpresa, CancellationToken cancellationToken = default);
    Task ActualizarValorAsync(string clave, int? idEmpresa, string nuevoValor, CancellationToken cancellationToken = default);
}
