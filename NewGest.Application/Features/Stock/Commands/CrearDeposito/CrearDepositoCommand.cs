using MediatR;
using NewGest.Application.DTOs.Stock;

namespace NewGest.Application.Features.Stock.Commands.CrearDeposito;

public record CrearDepositoCommand(int IdEmpresa, string Codigo, string Descripcion) : IRequest<DepositoDto>;
