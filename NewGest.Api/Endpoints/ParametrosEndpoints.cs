using MediatR;
using NewGest.Application.DTOs;
using NewGest.Application.Features.Parametros.Commands;
using NewGest.Application.Features.Parametros.Queries;
using NewGest.Application.Interfaces;

namespace NewGest.Api.Endpoints;

public static class ParametrosEndpoints
{
    public static IEndpointRouteBuilder MapParametrosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parametros")
            .WithTags("Parametros")
            .RequireAuthorization();

        // GET /api/parametros
        group.MapGet("/", async (ICurrentUserService currentUser, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetParametrosQuery(currentUser.IdEmpresa));
            return Results.Ok(result);
        })
        .WithName("GetParametros")
        .Produces<IReadOnlyList<ParametroDto>>();

        // PUT /api/parametros/{clave}
        group.MapPut("/{clave}", async (
            string clave,
            ActualizarParametroDto dto,
            ICurrentUserService currentUser,
            IMediator mediator) =>
        {
            await mediator.Send(new ActualizarParametroCommand(clave, currentUser.IdEmpresa, dto.Valor));
            return Results.NoContent();
        })
        .WithName("ActualizarParametro")
        .Produces(204)
        .ProducesProblem(404);

        return app;
    }
}
