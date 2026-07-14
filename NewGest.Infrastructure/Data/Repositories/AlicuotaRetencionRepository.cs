using Microsoft.EntityFrameworkCore;
using NewGest.Application.Interfaces;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class AlicuotaRetencionRepository : IAlicuotaRetencionRepository
{
    private readonly NewgestDbContext _db;

    public AlicuotaRetencionRepository(NewgestDbContext db) => _db = db;

    public async Task<decimal?> ObtenerAlicuotaVigenteAsync(
        int idEmpresa, TipoRetencion tipo, string? provincia, DateOnly fecha, CancellationToken ct)
    {
        return await _db.AlicuotasRetencion
            .Where(a =>
                a.IdEmpresa == idEmpresa &&
                a.TipoRetencion == tipo.ToString() &&
                a.Provincia == provincia &&
                a.VigenciaDesde <= fecha &&
                (a.VigenciaHasta == null || a.VigenciaHasta >= fecha))
            .OrderByDescending(a => a.VigenciaDesde)
            .Select(a => (decimal?)a.Porcentaje)
            .FirstOrDefaultAsync(ct);
    }
}
