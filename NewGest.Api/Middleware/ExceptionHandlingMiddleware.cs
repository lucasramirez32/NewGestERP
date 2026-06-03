using System.Net;
using System.Text.Json;
using FluentValidation;
using NewGest.Domain.Common;

namespace NewGest.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("DomainException: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await WriteJsonResponse(context, HttpStatusCode.UnprocessableEntity, new
            {
                message = "Error de validación.",
                errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Ocurrió un error interno.");
        }
    }

    private static Task WriteErrorResponse(HttpContext context, HttpStatusCode status, string message)
        => WriteJsonResponse(context, status, new { message });

    private static async Task WriteJsonResponse(HttpContext context, HttpStatusCode status, object body)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
