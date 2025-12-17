using Subify.Domain.Models.Entities.Common;
using Subify.Domain.Models.Entities.Users;

namespace Subify.Domain.Models.Entities.Subscriptions;

/// <summary>
/// User-defined custom categories. 
/// Examples: 'Gym', 'VPN', 'Domain Hosting'
/// </summary>
public sealed class UserCategory : BaseEntity
{
    public Guid? UserId { get; set; }

    /// <summary>
    /// Category name defined by user.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly slug (auto-generated from Name).
    /// Used for Resource lookup if user wants localization (optional).
    /// </summary>
    public string Slug { get; set; } = null!;

    /// <summary>
    /// Optional icon identifier. 
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Optional hex color code.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Display order in user's category list.
    /// </summary>
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ApplicationUser? User { get; set; } = null!;
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}