namespace Subify.Domain.Abstractions.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }

    bool IsDeleted => DeletedAt.HasValue;

    public void Delete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
    }

    public void UndoDelete()
    {
        DeletedAt = null;
    }
}
