using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class Category : BaseEntity, ISoftDeletable
{
    public string Slug { get; private set; } = null!;
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }

    protected Category()
    {
    }

    /// <summary>
    /// Creates a system catalog category (IsDefault=true). Used by seed (2.3.5).
    /// </summary>
    public static Category CreateSystem(string slug, string? icon, string? color, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var category = new Category
        {
            Id = GuidGenerator.NewId()
        };
        category.Apply(slug.Trim().ToLowerInvariant(), icon, color, sortOrder, isDefault: true, isActive: true);
        return category;
    }

    public void Create(string slug, string? icon, string? color, int sortOrder, bool isDefault)
    {
        Apply(slug, icon, color, sortOrder, isDefault, isActive: true);
    }

    public void Update(string slug, string? icon, string? color, int sortOrder, bool isDefault, bool isActive)
    {
        Apply(slug, icon, color, sortOrder, isDefault, isActive);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(
        string slug,
        string? icon,
        string? color,
        int sortOrder,
        bool isDefault,
        bool isActive)
    {
        Slug = slug;
        Icon = icon;
        Color = color;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = isActive;
        if (CreatedAt == default)
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}
