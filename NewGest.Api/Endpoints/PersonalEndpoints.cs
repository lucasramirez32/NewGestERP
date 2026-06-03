using MediatR;
using NewGest.Application.DTOs.Personal;
using NewGest.Application.DTOs.Shared;
using NewGest.Application.Features.Personal.Commands.ActualizarEmpleado;
using NewGest.Application.Features.Personal.Commands.ActualizarMutual;
using NewGest.Application.Features.Personal.Commands.ActualizarViaje;
using NewGest.Application.Features.Personal.Commands.CrearEmpleado;
using NewGest.Application.Features.Personal.Commands.CrearMutual;
using NewGest.Application.Features.Personal.Commands.CrearViaje;
using NewGest.Application.Features.Personal.Commands.DesactivarEmpleado;
using NewGest.Application.Features.Personal.Queries.GetEmpleadoById;
using NewGest.Application.Features.Personal.Queries.GetEmpleados;
using NewGest.Application.Features.Personal.Queries.GetMutuales;
using NewGest.Application.Features.Personal.Queries.GetVendedores;
using NewGest.Application.Features.Personal.Queries.GetViajes;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class PersonalEndpoints
{
    public static IEndpointRouteBuilder MapPersonalEndpoints(this IEndpointRouteBuilder app)
    {
        MapEmpleados(app);
        MapViajes(app);
        MapMutuales(app);
        return app;
    }

    private static void MapEmpleados(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/empleados")
            .WithTags("Personal - Empleados")
            .RequireAuthorization();

        // GET /api/empleados
        group.MapGet("/", async (
            string? search,
            bool? soloVendedores,
            int page,
            int pageSize,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetEmpleadosQuery(
                cu.IdEmpresa, search, soloVendedores,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetEmpleados")
        .Produces<PagedResult<EmpleadoListItemDto>>();

        // GET /api/empleados/{id}
        group.MapGet("/{id:int}", async (
            int id,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetEmpleadoByIdQuery(cu.IdEmpresa, id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetEmpleadoById")
        .Produces<EmpleadoDto>()
        .ProducesProblem(404);

        // POST /api/empleados
        group.MapPost("/", async (
            CrearEmpleadoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearEmpleadoCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/empleados/{result.IdEmpleado}", result);
        })
        .WithName("CrearEmpleado")
        .Produces<EmpleadoDto>(201)
        .ProducesProblem(400);

        // PUT /api/empleados/{id}
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarEmpleadoDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ActualizarEmpleadoCommand(cu.IdEmpresa, id, dto), ct);
            return Results.Ok(result);
        })
        .WithName("ActualizarEmpleado")
        .Produces<EmpleadoDto>()
        .ProducesProblem(400)
        .ProducesProblem(404);

        // DELETE /api/empleados/{id} — soft delete
        group.MapDelete("/{id:int}", async (
            int id,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DesactivarEmpleadoCommand(cu.IdEmpresa, id), ct);
            return Results.NoContent();
        })
        .WithName("DesactivarEmpleado")
        .Produces(204)
        .ProducesProblem(404);

        // GET /api/vendedores — lista plana de empleados vendedores para selects
        app.MapGet("/api/vendedores", async (
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetVendedoresQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        })
        .WithTags("Personal - Empleados")
        .RequireAuthorization()
        .WithName("GetVendedores")
        .Produces<IReadOnlyList<EmpleadoListItemDto>>();
    }

    private static void MapViajes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/viajes")
            .WithTags("Personal - Viajes")
            .RequireAuthorization();

        group.MapGet("/", async (ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetViajesQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        }).WithName("GetViajes").Produces<IReadOnlyList<ViajeDto>>();

        group.MapPost("/", async (CrearViajeDto dto, ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearViajeCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/viajes/{result.IdViaje}", result);
        }).WithName("CrearViaje").Produces<ViajeDto>(201);

        group.MapPut("/{id:int}", async (
            int id, ActualizarViajeDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ActualizarViajeCommand(cu.IdEmpresa, id, dto), ct);
            return Results.Ok(result);
        }).WithName("ActualizarViaje").Produces<ViajeDto>().ProducesProblem(404);
    }

    private static void MapMutuales(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mutuales")
            .WithTags("Personal - Mutuales")
            .RequireAuthorization();

        group.MapGet("/", async (ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new GetMutualesQuery(cu.IdEmpresa), ct);
            return Results.Ok(result);
        }).WithName("GetMutuales").Produces<IReadOnlyList<MutualDto>>();

        group.MapPost("/", async (CrearMutualDto dto, ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CrearMutualCommand(cu.IdEmpresa, dto), ct);
            return Results.Created($"/api/mutuales/{result.IdMutual}", result);
        }).WithName("CrearMutual").Produces<MutualDto>(201);

        group.MapPut("/{id:int}", async (
            int id, ActualizarMutualDto dto,
            ICurrentUserService cu, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new ActualizarMutualCommand(cu.IdEmpresa, id, dto), ct);
            return Results.Ok(result);
        }).WithName("ActualizarMutual").Produces<MutualDto>().ProducesProblem(404);
    }
}
