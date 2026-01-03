namespace Subify.Domain.Models.Entities.Common;

public class EmailTemplate: BaseEntity
{
    public string Name { get; set; } = null!;

    public string LanguageCode { get; set; } = "tr";

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;
}
