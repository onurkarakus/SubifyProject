using Subify.Domain.Enums;

namespace Subify.Domain.Abstractions.Services;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body);

    public Task GetEmailTemplateAndSendAsync(EmailType emailType, string token, string locale, string userId, string to, Dictionary<string, string> replacements);

}
