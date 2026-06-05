using MediatR;
using NewGest.Application.Features.Contabilidad.EventHandlers;
using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Domain.Events;

namespace NewGest.Infrastructure.Services;

/// <summary>
/// Adapta IDomainEvent → INotification para que MediatR pueda despacharlos.
/// Solo mapea los eventos conocidos; eventos sin handler se ignoran silenciosamente.
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator) => _mediator = mediator;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
    {
        foreach (var @event in events)
        {
            var notification = ToNotification(@event);
            if (notification is not null)
                await _mediator.Publish(notification, ct);
        }
    }

    private static INotification? ToNotification(IDomainEvent @event) => @event switch
    {
        CaeAsignadoEvent e => new CaeAsignadoNotification(e.IdComprobante, e.IdEmpresa, e.CodigoCae),
        _ => null
    };
}
