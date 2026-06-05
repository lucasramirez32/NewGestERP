using NewGest.Application.Interfaces;
using NewGest.Domain.Common;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NewgestDbContext _db;
    private readonly IDomainEventDispatcher _dispatcher;

    public UnitOfWork(NewgestDbContext db, IDomainEventDispatcher dispatcher)
    {
        _db = db;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Persiste cambios y luego despacha domain events de todos los aggregates rastreados.
    /// Los eventos se despachan DESPUÉS del commit para que los handlers asuman
    /// datos ya persistidos. Si SaveChanges falla, los handlers no se ejecutan.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        var aggregates = _db.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        var result = await _db.SaveChangesAsync(ct);

        foreach (var a in aggregates)
            a.ClearDomainEvents();

        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, ct);

        return result;
    }
}
