using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, SubifyDbContext dbContext)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task GetEmailTemplateAndSendAsync(EmailType emailType, string token, string locale, string userId, string to, Dictionary<string, string> replacements)
    {
        var emailTemplate = await _dbContext.EmailTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                t.LanguageCode == locale && t.Name == emailType.ToString());

        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:3000";
        var userClickLink = string.Empty;
        var subject = string.Empty;

        switch (emailType)
        {
            case EmailType.VerifyEmail:
                userClickLink = $"{frontendUrl}/verify-email?userId={userId}&token={encodedToken}";
                break;
            case EmailType.ForgotPassword:
                userClickLink = $"{frontendUrl}/reset-password?email={to}&token={encodedToken}";
                break;
            default:
                break;
        }

        if (emailTemplate != null)
        {
            var body = emailTemplate.Body;

            foreach (var replacement in replacements)
            {
                body = body.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
            }

            body.Replace("{{USER_LINK}}", userClickLink);

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

            return; // Veya throw new Exception(...)
        }

        // --- SDK KULLANIMI ÖRNEÐÝ ---
        // Burada Resend SDK veya HttpClient ile post iþlemi yapýlýr.
        // Örnek (Pseudo-code):
        /*
        var message = new EmailMessage();
        message.From = "onboarding@subify.app";
        message.To.Add(to);
        message.Subject = subject;
        message.HtmlBody = body;
        
        await _resendClient.Email.SendAsync(message);
        */

        // Geçici log (Implementasyon yapýlana kadar kodu kýrmasýn diye)
        _logger.LogInformation($"[RESEND] E-posta gönderildi -> Kime: {to}, Konu: {subject}");

        await Task.CompletedTask;
    }


}
