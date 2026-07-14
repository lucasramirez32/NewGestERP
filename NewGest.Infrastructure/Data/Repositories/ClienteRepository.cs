using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly NewgestDbContext _db;

    public ClienteRepository(NewgestDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ClienteListItemDto>> GetPagedAsync(
        int idEmpresa,
        string? search,
        CondicionIva? condicionIva,
        int? idZona,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.Clientes
            .Where(c => c.IdEmpresa == idEmpresa);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpper();
            query = query.Where(c =>
                c.Codigo.Contains(s) ||
                c.RazonSocial.Contains(search) ||
                (c.NombreFantasia != null && c.NombreFantasia.Contains(search)) ||
                (c.CUIT != null && c.CUIT.Contains(s)));
        }

        if (condicionIva.HasValue)
            query = query.Where(c => c.CondicionIva == condicionIva.Value);

        if (idZona.HasValue)
            query = query.Where(c => c.IdZona == idZona.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.RazonSocial)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClienteListItemDto(
                c.IdCliente,
                c.Codigo.Trim(),
                c.RazonSocial,
                c.CUIT,
                c.CondicionIva,
                c.CondicionIva.ToString(),
                c.Localidad,
                c.IdZona,
                c.Activo,
                c.NombreFantasia))
            .ToListAsync(ct);

        return new PagedResult<ClienteListItemDto>(items, total, page, pageSize);
    }

    public Task<Cliente?> GetByIdAsync(int idEmpresa, int idCliente, CancellationToken ct)
        => _db.Clientes
            .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa && c.IdCliente == idCliente, ct);

    public Task<bool> ExisteCuitAsync(int idEmpresa, string cuit, int? excludeId, CancellationToken ct)
        => _db.Clientes
            .Where(c => c.IdEmpresa == idEmpresa && c.CUIT == cuit)
            .Where(c => excludeId == null || c.IdCliente != excludeId)
            .AnyAsync(ct);

    public Task<bool> ExisteCodigoAsync(int idEmpresa, string codigo, int? excludeId, CancellationToken ct)
    {
        var codigoUpper = codigo.Trim().ToUpper();
        return _db.Clientes
            .Where(c => c.IdEmpresa == idEmpresa && c.Codigo.Trim() == codigoUpper)
            .Where(c => excludeId == null || c.IdCliente != excludeId)
            .AnyAsync(ct);
    }

    public async Task AddAsync(Cliente cliente, CancellationToken ct)
        => await _db.Clientes.AddAsync(cliente, ct);

    public void Update(Cliente cliente)
        => _db.Clientes.Update(cliente);
}
