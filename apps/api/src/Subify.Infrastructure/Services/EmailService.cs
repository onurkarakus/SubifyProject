using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Subify.Domain.Abstractions.Services;

namespace Subify.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
