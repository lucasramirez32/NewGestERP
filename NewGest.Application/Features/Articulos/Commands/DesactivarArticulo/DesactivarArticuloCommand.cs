using MediatR;

namespace NewGest.Application.Features.Articulos.Commands.DesactivarArticulo;

public record DesactivarArticuloCommand(int IdEmpresa, int IdArticulo) : IRequest;
