using Subify.Domain.Abstractions.Common;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class Category : BaseEntity, ISoftDeletable
{
    public string Slug { get; private set; }
    public string? Icon { get; private set; }
    public string? Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeletedAt { get; set; }

    protected Category() { }

    public void Create(string slug, string? icon, string? color, int sortOrder, bool isDefault)
    {
        Slug = slug;
        Icon = icon;
        Color = color;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string slug, string? icon, string? color, int sortOrder, bool isDefault, bool isActive)
    {
        Slug = slug;
        Icon = icon;
        Color = color;
        SortOrder = sortOrder;
        IsDefault = isDefault;
        IsActive = isActive;
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
}