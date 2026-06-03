using MediatR;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Features.Comprobantes.Queries.GetComprobantes;
using NewGest.Application.Interfaces;

namespace NewGest.Application.Features.Comprobantes.Queries.GetComprobanteById;

public class GetComprobanteByIdQueryHandler : IRequestHandler<GetComprobanteByIdQuery, ComprobanteDto?>
{
    private readonly IComprobanteRepository _repo;

    public GetComprobanteByIdQueryHandler(IComprobanteRepository repo) => _repo = repo;

    public async Task<ComprobanteDto?> Handle(GetComprobanteByIdQuery request, CancellationToken ct)
    {
        var comprobante = await _repo.GetByIdAsync(request.IdComprobante, request.IdEmpresa, ct);
        return comprobante is null ? null : GetComprobantesQueryHandler.ToDto(comprobante);
    }
}
