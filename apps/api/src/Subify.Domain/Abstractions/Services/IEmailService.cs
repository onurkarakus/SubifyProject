using System;

namespace Subify.Domain.Abstractions.Services;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body);

}
