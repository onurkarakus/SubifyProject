namespace Subify.Application.Common.Interfaces;

/// <summary>
/// Single commit boundary for a request-scoped unit of work (task 2.4.2).
/// Prefer one <see cref="SaveChangesAsync"/> at the end of a handler after all entity changes.
/// </summary>
/// <remarks>
/// Implemented by <c>SubifyDbContext</c> (also via <see cref="ISubifyDbContext"/>).
/// Save path always applies UUID v7 fill, audit timestamps, and soft-delete conversion.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all tracked changes in the current DI scope as one database transaction unit
    /// (EF Core default transaction per SaveChanges call).
    /// </summary>
    /// <returns>Number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
