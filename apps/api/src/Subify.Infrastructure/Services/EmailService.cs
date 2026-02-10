using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using Subify.Domain.Abstractions.Services;
using Subify.Domain.Enums;
using Subify.Infrastructure.Persistence;
using System.Threading;

namespace Subify.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SubifyDbContext _dbContext;
    private readonly IResend _resend;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, SubifyDbContext dbContext, IResend resend)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _resend = resend;
    }

    public async Task GetEmailTemplateAndSendAsync(EmailType emailType, string token, string locale, string userId, string to, Dictionary<string, string> replacements)
    {
        var emailTemplate = await _dbContext.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                t.LanguageCode == locale && t.Name == emailType.ToString());

        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:3000";
        var subject = string.Empty;
        
        if (emailTemplate != null)
        {
            var body = emailTemplate.Body;

            foreach (var replacement in replacements)
            {
                body = body.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
            }

            switch (emailType)
            {
                case EmailType.VerifyEmail:
                    body = body.Replace("{{USER_LINK}}", $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}");
                    body = body.Replace("{{VERIFICATION_LINK}}", $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}");
                    break;
                case EmailType.ForgotPassword:
                    body = body.Replace("{{USER_LINK}}", $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}");
                    body = body.Replace("{{RESET_LINK}}", $"{frontendUrl}/reset-password?email={to}&token={encodedToken}");
                    break;
                default:
                    break;
            }

            await SendEmailAsync(to, emailTemplate.Subject, body);
        }
        else
        {
            var body = $"Hello, <br> Please verify your email by clicking <a href='{frontendUrl}/verify-email?userId={userId}&token={encodedToken}'>here</a>.";
            await SendEmailAsync(to, "", body);
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var apiKey = _configuration["Resend:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogError("Resend API Key bulunamadý! E-posta gönderilemedi.");

            return; 
        }

        var message = new EmailMessage();
        message.From = "Acme <onboarding@resend.dev>";
        message.To.Add(to);
        message.Subject = subject;
        message.HtmlBody = body;
        
        await _resend.EmailSendAsync(message);
        

        // Geçici log (Implementasyon yapýlana kadar kodu kýrmasýn diye)
        _logger.LogInformation($"[RESEND] E-posta gönderildi -> Kime: {to}, Konu: {subject}");

        await Task.CompletedTask;
    }


}
