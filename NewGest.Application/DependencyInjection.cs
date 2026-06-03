using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.Common;

namespace NewGest.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Pipeline de validación: ejecuta FluentValidation antes de cada handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
