namespace Subify.Domain.Models.Entities.Common;

public sealed class Resource : BaseEntity
{
    /// <summary>
    /// Logical grouping for client-side namespacing.
    /// Examples: 'Common', 'Category', 'Subscription', 'Error', 'Validation'
    /// </summary>
    public string PageName { get; set; } = null!;

    /// <summary>
    /// Resource key within the page.
    /// Examples: 'Music', 'Streaming', 'SaveButton', 'RequiredField'
    /// Client uses: {PageName}. {Name} → 'Category.Music'
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// ISO 639-1 language code.
    /// Examples: 'TR', 'EN'
    /// </summary>
    public string LanguageCode { get; set; } = null!;

    /// <summary>
    /// The localized text value.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Indicates whether the resource is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
