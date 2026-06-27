using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Subify.Domain.Common;

namespace Subify.Domain.Entities;

public class EmailTemplates: BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    protected EmailTemplates() { }

    public EmailTemplates(string name, string languageCode, string subject, string body)
    {
        Name = name;
        LanguageCode = languageCode;
        Subject = subject;
        Body = body;
    }

    public void Update(string name, string languageCode, string subject, string body)
    {
        Name = name;
        LanguageCode = languageCode;
        Subject = subject;
        Body = body;
    }    
}