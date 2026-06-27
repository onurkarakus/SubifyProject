using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class Resource : BaseEntity
{
    public string PageName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    protected Resource() { }

    public void Create(string pageName, string name, string languageCode, string value)
    {
        PageName = pageName;
        Name = name;
        LanguageCode = languageCode;
        Value = value;
    }

    public void Update(string pageName, string name, string languageCode, string value)
    {
        PageName = pageName;
        Name = name;
        LanguageCode = languageCode;
        Value = value;
    }
}