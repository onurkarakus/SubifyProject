using Subify.Domain.Abstractions.Common;
using Subify.Domain.Models.Entities.Common;

namespace Subify.Domain.Models.Entities.Providers;

/// <summary>
/// Represents a subscription provider with details such as name, pricing, region, and other metadata.
/// </summary>
public class Provider : BaseEntity, ISoftDeletable
{
    /// <summary>
    /// Gets or sets the name of the provider.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the unique slug identifier for the provider.
    /// </summary>
    public string Slug { get; set; }

    /// <summary>
    /// Gets or sets the URL of the provider's logo.
    /// </summary>
    public string LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the WebSite URL for additional information about the provider.
    /// </summary>
    public string Website { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is considered popular.
    /// </summary>
    public bool IsPopular { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the provider was soft-deleted. Null if not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
