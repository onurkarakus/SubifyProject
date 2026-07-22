# Persistence

## Unit of work (task 2.4.2)

| Abstraction | Role |
| ----------- | ---- |
| `IUnitOfWork` | Commit only — `SaveChangesAsync` |
| `ISubifyDbContext` | `IUnitOfWork` + `DbSet<>` surface for handlers |
| `SubifyDbContext` | EF implementation; **one scoped instance** per request |

### Handler convention

```csharp
public sealed class CreateSubscriptionHandler(...)
{
    public async Task<Result<...>> Handle(..., CancellationToken ct)
    {
        var entity = Subscription.Create(...);
        if (entity.IsFailure) return entity.Error;

        await _db.Subscriptions.AddAsync(entity.Value, ct);
        // other tracked changes...
        await _db.SaveChangesAsync(ct); // single commit
        return Result.Success(...);
    }
}
```

**Rules**

1. Prefer **one** `SaveChangesAsync` at the end of a use case.
2. Do not call `SaveChanges` from domain entities.
3. Seeders/Infrastructure may call `SubifyDbContext.SaveChangesAsync` directly.
4. `AddRefreshTokenAsync` is an auth convenience that commits immediately (single-entity flow).

### Save pipeline (always)

`PrepareChangesForSave` before SQL:

1. Empty `Id` → UUID v7  
2. `CreatedAt` / `UpdatedAt`  
3. Hard delete on `ISoftDeletable` → soft delete / `Subscription.Archive`
