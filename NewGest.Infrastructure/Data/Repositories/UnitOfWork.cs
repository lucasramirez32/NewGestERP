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
    /// Persiste cambios y despacha domain events dentro de una única transacción de BD.
    /// Flujo:
    ///   1. BEGIN TRANSACTION
    ///   2. SaveChanges  (persiste el aggregate principal, ej: Comprobante)
    ///   3. Despachar domain events (los handlers pueden hacer AddAsync de entidades nuevas, ej: Asiento)
    ///   4. SaveChanges  (persiste los side-effects de los handlers)
    ///   5. COMMIT
    /// Si cualquier paso falla, ROLLBACK garantiza atomicidad total.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        var aggregates = _db.ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // Sin events: commit simple sin transacción explícita (path habitual)
        if (events.Count == 0)
            return await _db.SaveChangesAsync(ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await _db.SaveChangesAsync(ct);

            foreach (var a in aggregates)
                a.ClearDomainEvents();

            // Los handlers pueden AddAsync nuevas entidades (ej: Asiento);
            // el segundo SaveChanges las persiste dentro de la misma transacción.
            await _dispatcher.DispatchAsync(events, ct);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
