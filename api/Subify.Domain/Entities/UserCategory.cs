using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class UserCategory : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; } = string.Empty;
    public string? Color { get; private set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }

    public ApplicationUser User { get; private set; } = null!;

    protected UserCategory() { }

    /// <summary>Creates a personal category owned by <paramref name="userId"/>.</summary>
    public static UserCategory CreateForUser(Guid userId, string name, string? icon = null, string? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var category = new UserCategory
        {
            Id = GuidGenerator.NewId()
        };
        category.Create(userId, name.Trim(), icon, color);
        return category;
    }

    public void Create(Guid userId, string name, string? icon, string? color)
    {
        UserId = userId;
        Name = name;
        Icon = icon;
        Color = color;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? icon, string? color)
    {
        Name = name;
        Icon = icon;
        Color = color;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}