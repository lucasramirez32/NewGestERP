using MediatR;
using NewGest.Application.DTOs.Clientes;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Clientes.Commands.ActualizarCliente;
using NewGest.Application.Features.Clientes.Commands.CrearCliente;
using NewGest.Application.Features.Clientes.Commands.DesactivarCliente;
using NewGest.Application.Features.Clientes.Queries.GetClienteById;
using NewGest.Application.Features.Clientes.Queries.GetClientes;
using NewGest.Application.Interfaces;
using NewGest.Domain.Enums;

namespace NewGest.Api.Endpoints;

public static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .RequireAuthorization();

        // GET /api/clientes
        group.MapGet("/", async (
            string? search,
            int? condicionIva,
            int? zona,
            int page,
            int pageSize,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetClientesQuery(
                currentUser.IdEmpresa,
                search,
                condicionIva.HasValue ? (CondicionIva)condicionIva.Value : null,
                zona,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize);

            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetClientes")
        .Produces<PagedResult<ClienteListItemDto>>();

        // GET /api/clientes/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetClienteByIdQuery(currentUser.IdEmpresa, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetClienteById")
        .Produces<ClienteDto>()
        .ProducesProblem(404);

        // POST /api/clientes
        group.MapPost("/", async (
            CrearClienteDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CrearClienteCommand(
                currentUser.IdEmpresa,
                dto.Codigo,
                dto.RazonSocial,
                dto.CUIT,
                dto.CondicionIva,
                dto.Domicilio,
                dto.Localidad,
                dto.Telefono,
                dto.Email,
                dto.IdZona,
                dto.Observaciones);

            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/clientes/{result.IdCliente}", result);
        })
        .WithName("CrearCliente")
        .Produces<ClienteDto>(201)
        .ProducesProblem(400)
        .ProducesProblem(422);

        // PUT /api/clientes/{id}
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarClienteDto dto,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new ActualizarClienteCommand(
                currentUser.IdEmpresa,
                id,
                dto.RazonSocial,
                dto.CUIT,
                dto.CondicionIva,
                dto.Domicilio,
                dto.Localidad,
                dto.Telefono,
                dto.Email,
                dto.IdZona,
                dto.Observaciones);

            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ActualizarCliente")
        .Produces<ClienteDto>()
        .ProducesProblem(400)
        .ProducesProblem(404);

        // DELETE /api/clientes/{id}  — soft delete
        group.MapDelete("/{id:int}", async (
            int id,
            ICurrentUserService currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DesactivarClienteCommand(currentUser.IdEmpresa, id), ct);
            return Results.NoContent();
        })
        .WithName("DesactivarCliente")
        .Produces(204)
        .ProducesProblem(404);

        // GET /api/clientes/{id}/saldo-pendiente — stub Sprint 12
        group.MapGet("/{id:int}/saldo-pendiente", (int id, ICurrentUserService _) =>
            Results.Ok(new { saldoPendiente = 0m }))
        .WithName("GetSaldoPendienteCliente")
        .Produces<object>();

        return app;
    }
}
