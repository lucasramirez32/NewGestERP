using NewGest.Application.DTOs.Pedidos;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;

namespace NewGest.Application.Interfaces;

public interface IPedidoRepository
{
    Task<PagedResult<PedidoListItemDto>> GetPagedAsync(int idEmpresa, EstadoPedido? estado, int page, int pageSize, CancellationToken ct);
    Task<Pedido?> GetByIdAsync(int idEmpresa, int idPedido, CancellationToken ct);
    Task AddAsync(Pedido pedido, CancellationToken ct);
    Task AddRemitoAsync(Remito remito, CancellationToken ct);
}
