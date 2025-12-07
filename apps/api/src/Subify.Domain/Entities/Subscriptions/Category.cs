using Subify.Domain.Abstractions.Common;
using Subify.Domain.Entities.Common;

namespace Subify.Domain.Entities.Subscriptions;

public sealed class Category : BaseEntity
{
    /// <summary>
    /// URL-friendly unique identifier.
    /// Examples: 'streaming', 'music', 'productivity'
    /// Used as Resource.Name for i18n lookup: PageName='Category', Name=Slug
    /// </summary>
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Icon identifier for frontend rendering.
    /// Examples: 'play-circle', 'music', 'briefcase'
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Hex color code for UI theming.
    /// Example: '#E50914'
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Display order in category lists.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// System-defined categories are active by default.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
