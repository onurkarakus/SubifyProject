namespace Subify.Domain.Abstractions.Common;
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
}
