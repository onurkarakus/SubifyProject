using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Subify.Domain.Abstractions.Services;

namespace Subify.Application.Services;

public class EmailService : IEmailService
{  

    public EmailService()
    {
   
    }

    public Task<bool> SendVerificationEmailAsync(string to, string verificationToken)
    {
        



        return Task.FromResult(true);
    }
}
