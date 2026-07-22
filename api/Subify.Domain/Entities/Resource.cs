using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class Resource : BaseEntity
{
    public string PageName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    protected Resource()
    {
    }

    /// <summary>
    /// Creates an i18n resource row for seed (task 2.3.7).
    /// Unique key: (PageName, Name, LanguageCode).
    /// </summary>
    public static Resource Create(string pageName, string name, string languageCode, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var resource = new Resource
        {
            Id = GuidGenerator.NewId(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        resource.Apply(
            pageName.Trim(),
            name.Trim(),
            languageCode.Trim().ToLowerInvariant(),
            value);
        return resource;
    }

    public void CreateFields(string pageName, string name, string languageCode, string value)
    {
        Apply(pageName, name, languageCode, value);
    }

    public void Update(string pageName, string name, string languageCode, string value)
    {
        Apply(pageName, name, languageCode, value);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void Apply(string pageName, string name, string languageCode, string value)
    {
        PageName = pageName;
        Name = name;
        LanguageCode = languageCode;
        Value = value;
    }
}
