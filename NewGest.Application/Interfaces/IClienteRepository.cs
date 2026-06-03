using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;

namespace NewGest.Application.Interfaces;

public interface IClienteRepository
{
    Task<PagedResult<ClienteListItemDto>> GetPagedAsync(
        int idEmpresa,
        string? search,
        CondicionIva? condicionIva,
        int? idZona,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<Cliente?> GetByIdAsync(int idEmpresa, int idCliente, CancellationToken ct);
    Task<bool> ExisteCuitAsync(int idEmpresa, string cuit, int? excludeId, CancellationToken ct);
    Task<bool> ExisteCodigoAsync(int idEmpresa, string codigo, int? excludeId, CancellationToken ct);
    Task AddAsync(Cliente cliente, CancellationToken ct);
    void Update(Cliente cliente);
}
