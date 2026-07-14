using NewGest.Domain.Enums;

namespace NewGest.Application.Interfaces;

public interface IAlicuotaRetencionRepository
{
    Task<decimal?> ObtenerAlicuotaVigenteAsync(
        int idEmpresa, TipoRetencion tipo, string? provincia, DateOnly fecha, CancellationToken ct);
}
