using NewGest.Application.Interfaces;
using NewGest.Infrastructure.Data;

namespace NewGest.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NewgestDbContext _db;

    public UnitOfWork(NewgestDbContext db)
    {
        _db = db;
    }

    public Task<int> CommitAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
