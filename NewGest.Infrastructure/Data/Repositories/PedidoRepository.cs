using Microsoft.EntityFrameworkCore;
using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly NewgestDbContext _db;

    public PedidoRepository(NewgestDbContext db) => _db = db;

    public async Task<PagedResult<PedidoListItemDto>> GetPagedAsync(
        int idEmpresa,
        EstadoPedido? estado,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = _db.Pedidos
            .Where(p => p.IdEmpresa == idEmpresa);

        if (estado.HasValue)
            query = query.Where(p => p.Estado == estado.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.FechaPedido)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PedidoListItemDto(
                p.IdPedido,
                p.IdCliente,
                string.Empty,    // sin join pesado — se puede enriquecer con Dapper si hace falta
                p.IdVendedor,
                null,
                p.Estado,
                p.Estado.ToString(),
                p.FechaPedido,
                p.FechaEntregaEstimada,
                p.Items.Count))
            .ToListAsync(ct);

        return new PagedResult<PedidoListItemDto>(items, total, page, pageSize);
    }

    public Task<Pedido?> GetByIdAsync(int idEmpresa, int idPedido, CancellationToken ct)
        => _db.Pedidos
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.IdPedido == idPedido, ct);

    public async Task AddAsync(Pedido pedido, CancellationToken ct)
        => await _db.Pedidos.AddAsync(pedido, ct);

    public async Task AddRemitoAsync(Remito remito, CancellationToken ct)
        => await _db.Remitos.AddAsync(remito, ct);
}
